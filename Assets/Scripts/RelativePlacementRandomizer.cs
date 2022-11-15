using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers.Tags;
using UnityEngine.Perception.Randomization.Samplers;
using Newtonsoft.Json;

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Randomizes the position and rotation of objects tagged with a RelativePlacementRandomizerTag
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("dfigu/Relative Placement Randomizer")]
    public class RelativePlacementRandomizer : Randomizer
    {
        [Tooltip("The range of random rotations to assign to target objects [°].")]
        public Vector3Parameter rotationParameter = new Vector3Parameter
        {
            x = new UniformSampler(0, 360),
            y = new UniformSampler(0, 360),
            z = new UniformSampler(0, 360)
        };

        [Tooltip("The range of random positions to assign to target objects [m].")]
        public Vector3Parameter positionParameter = new Vector3Parameter
        {
            x = new UniformSampler(-1, 1),
            y = new UniformSampler(-1, 1),
            z = new UniformSampler(-1, 1)
        };

        [Tooltip("The number of background images provided.")]
        public int backroundImageNumber;

        [Tooltip("The number of total iterations in the scenario.")]
        public int scenarioIterationNumber;

        [Tooltip("The tolerance for face occlusion detection.")]
        public double occlusionTolerance;

        private Dictionary<string, Dictionary<string, Dictionary<string, float>>> referencePoses;
        private Dictionary<string, Dictionary<string, float>> poseCorrections =  new Dictionary<string, Dictionary<string, float>>{
            {"2-slot", new Dictionary<string, float>{{"rotation", 0.0f}, {"translation", 0.0275f}}},
            {"3-slot", new Dictionary<string, float>{{"rotation", 0.0f}, {"translation", 0.0275f}}},
            {"arrowbutton", new Dictionary<string, float>{{"rotation", -14.17f}, {"translation", 0.0138f}}},
            {"redbutton", new Dictionary<string, float>{{"rotation", -14.17f}, {"translation", 0.0138f}}},
            {"mushroombutton", new Dictionary<string, float>{{"rotation", -12.74f}, {"translation", 0.0195f}}}
        };

        /*  Old pose corrections
            {"M4x40", new Dictionary<string, float>{{"rotation", 88.5679f}, {"translation", 0.0035f}}},
            {"M6x30", new Dictionary<string, float>{{"rotation", 86.1859f}, {"translation", 0.0041f}}},
            {"M8x16", new Dictionary<string, float>{{"rotation", 79.3803f}, {"translation", 0.0054f}}},
            {"M8x25", new Dictionary<string, float>{{"rotation", 84.2894f}, {"translation", 0.0052f}}},
            {"M8x50", new Dictionary<string, float>{{"rotation", 87.1376f}, {"translation", 0.0052f}}},
        */

        private int iterationNumber = 0;
        private bool notVisibleFacePlaced = false;

        protected override void OnIterationStart() 
        {
            var tags = tagManager.Query<RelativePlacementRandomizerTag>();

            List<RelativePlacementRandomizerTag> orderedTags = orderTags(tags);

            if(iterationNumber == 0){
                using(StreamReader r = new StreamReader("poses_azure.json")){
                    string text = r.ReadToEnd();
                    referencePoses = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, float>>>>(text);
                }
            }
            
            int imageNumber = (int)System.Math.Floor((double)iterationNumber*backroundImageNumber/scenarioIterationNumber);

            var rotation = referencePoses[imageNumber.ToString()]["rotation"];
            var translation = referencePoses[imageNumber.ToString()]["translation"];

            List<RelativePlacementRandomizerTag> placedTags = new List<RelativePlacementRandomizerTag>();

            notVisibleFacePlaced = false;
            int maxAttempts = 20;
            Debug.Log(iterationNumber);
            foreach(var tag in orderedTags){
                bool placed = placeObject(tag, placedTags, referencePoses[imageNumber.ToString()], maxAttempts);
                if(placed){
                    placedTags.Add(tag);
                }
            }

            iterationNumber++;
            
        }

        private bool placeObject(RelativePlacementRandomizerTag tag, 
                                List<RelativePlacementRandomizerTag> placedTags, 
                                Dictionary<string, Dictionary<string, float>> referencePose, 
                                int maxAttempts){

            // Makes maxAttempts attempts to place the object corresponding to the specified tag relative to the given reference system.
            // Returns true if successful, false otherwise.

            bool invalid = true;
            int currentAttempt = 0;

            while(invalid && currentAttempt < maxAttempts){
                
                // Placing object in a position
                placeObjectOnSurface(tag, referencePose);
                    
                // Verifying the position does not intersect with previously placed objects.
                invalid = checkIntersection(tag, placedTags);

                // Additional rotation around the x-axis for buttons where this creates a noticeable visual difference.
                if((tag.name == "arrowbutton" || tag.name == "mushroombutton") && !invalid){
                    var xrotation = new UniformSampler(0, 360);
                    Vector3 randomXRotation = new Vector3(xrotation.Sample(), 0, 0);
                    tag.transform.rotation *= Quaternion.Euler(randomXRotation);                    
                }
                    
                // Check if button faces are visible
                if((tag.name == "arrowbutton" || tag.name == "redbutton") && !invalid){
                    bool occluded = checkButtonFaceOccluded(tag, placedTags);
                    Debug.Log(tag.name);
                    Debug.Log(occluded);
                    
                    if(occluded && notVisibleFacePlaced){
                        // Limiting to one occluded button per image, necessary for EfficientPose's implementation.
                        invalid = true;
                    }
                    else if(occluded){
                        notVisibleFacePlaced = true;
                    }
                }

                currentAttempt++;
            }

            if(currentAttempt < maxAttempts){
                return true;
            }
            else{
                // Placing object far in scene, so it is not captured by camera.
                tag.transform.position = new Vector3(100, 0, 0);
                return false;
            }
        }

        private void placeObjectOnSurface(RelativePlacementRandomizerTag tag, Dictionary<string, Dictionary<string, float>> referencePose){
            // Places the object corresponging to the tag as if it was settled on a surface specified by referencePose.

            var rotation = referencePose["rotation"];
            var translation = referencePose["translation"];

            float qx = rotation["x"];
            float qy = rotation["y"];
            float qz = rotation["z"];
            float qw = rotation["w"];
            
            tag.transform.rotation = new Quaternion(qx, qy, qz, qw);
            tag.transform.rotation *= Quaternion.Euler(rotationParameter.Sample());

            Vector3 objectPosition = new Vector3(translation["x"], translation["y"], translation["z"]);
            Vector3 relativePosition = positionParameter.Sample();

            if(poseCorrections.ContainsKey(tag.name)){
                Vector3 rotationCorrection = new Vector3(0.0f, 0.0f, 0.0f);
                rotationCorrection.y += poseCorrections[tag.name]["rotation"];
                relativePosition.z += poseCorrections[tag.name]["translation"];
                tag.transform.rotation *= Quaternion.Euler(rotationCorrection);
            }
                
            float R11 = 1.0f - 2.0f * (qy * qy + qz * qz);
            float R12 = 2.0f * (qx * qy - qz * qw);
            float R13 = 2.0f * (qx * qz + qy * qw);
            float R21 = 2.0f * (qx * qy + qz * qw);
            float R22 = 1.0f - 2.0f * (qx * qx + qz * qz);
            float R23 = 2.0f * (qy * qz - qx * qw);
            float R31 = 2.0f * (qx * qz - qy * qw);
            float R32 = 2.0f * (qy * qz + qx * qw);
            float R33 = 1.0f - 2.0f * (qx * qx + qy * qy);

            float absX = R11 * relativePosition.x + R12*relativePosition.y +  R13*relativePosition.z;
            float absY = R21 * relativePosition.x + R22*relativePosition.y +  R23*relativePosition.z;
            float absZ = R31 * relativePosition.x + R32*relativePosition.y +  R33*relativePosition.z;

            objectPosition += new Vector3(absX, absY, absZ);
            tag.transform.position = objectPosition;
        }

        private List<RelativePlacementRandomizerTag> orderTags(IEnumerable<RelativePlacementRandomizerTag> tags){
            //Sorts a list of tags according to the specified order.
            //Currently o(n^2), but number of tags is not expected to be high.

            Dictionary<string, int> order = new Dictionary<string, int>{
                {"3-slot", 0},
                {"2-slot", 1},
                {"mushroombutton", 2},
                {"arrowbutton", 3},
                {"redbutton", 4}
            };

            List<RelativePlacementRandomizerTag> ordered = new List<RelativePlacementRandomizerTag>();

            while(ordered.Count < order.Count){
                foreach(var tag in tags){
                    if(order[tag.name] == ordered.Count){
                        ordered.Add(tag);
                    }
                }
            }

            return ordered;
        }

        private bool checkIntersection(RelativePlacementRandomizerTag tag, List<RelativePlacementRandomizerTag> placedObjects){
            // Checks whether the object corresponding to a tag intersects with already placed objects in the scene.
            // Returns true if there is an intersection, false otherwise.

            bool intersection = false;

            if(placedObjects.Count != 0){
                foreach(var placedObject in placedObjects){
                    if(tag.GetComponent<Renderer>().bounds.Intersects(placedObject.GetComponent<Renderer>().bounds)){
                        intersection = true;
                        break;
                    }
                }
            }

            return intersection;
        }

        private bool checkButtonFaceOccluded(RelativePlacementRandomizerTag button, List<RelativePlacementRandomizerTag> placedObjects){
            // Checks whether the face of a button is visible from the camera using the law of cosines.
            // Faces with gamma > 90 degrees - tolerance are considered occluded
            // Returns true if occluded, false otherwise.

            float b = 0.014f;

            float qx = button.transform.rotation.x;
            float qy = button.transform.rotation.y;
            float qz = button.transform.rotation.z;
            float qw = button.transform.rotation.w;

            float R11 = 1.0f - 2.0f * (qy * qy + qz * qz);
            float R21 = 2.0f * (qx * qy + qz * qw);
            float R31 = 2.0f * (qx * qz - qy * qw);

            Vector3 absoluteTranslationToFace = new Vector3(R11 * b, R21 * b, R31 * b);
            Vector3 FaceCenterPosition = button.transform.position + absoluteTranslationToFace;

            float c = button.transform.position.magnitude;
            float a = FaceCenterPosition.magnitude;

            double gamma = System.Math.Acos((double)(c*c-a*a-b*b)/(-2*a*b));
            Debug.Log(gamma);

            return gamma < System.Math.PI/2 - occlusionTolerance/360*2*System.Math.PI;
        }
    }
}
