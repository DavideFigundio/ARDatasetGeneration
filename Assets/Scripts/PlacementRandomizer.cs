using System;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers.Tags;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Randomizes the position and rotation of objects tagged with a PlacementRandomizerTag
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("dfigu/Placement Randomizer")]
    public class PlacementRandomizer : Randomizer
    {
        /// <summary>
        /// The range of random rotations to assign to target objects
        /// </summary>
        [Tooltip("The range of random rotations to assign to target objects [°].")]
        public Vector3Parameter rotation = new Vector3Parameter
        {
            x = new UniformSampler(0, 360),
            y = new UniformSampler(0, 360),
            z = new UniformSampler(0, 360)
        };

        [Tooltip("The range of random positions to assign to target objects [m].")]
        public Vector3Parameter position = new Vector3Parameter
        {
            x = new UniformSampler(-100, 100),
            y = new UniformSampler(-100, 100),
            z = new UniformSampler(-100, 100)
        };

        /// <summary>
        /// Randomizes the rotation of tagged objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var tags = tagManager.Query<PlacementRandomizerTag>();
            var assemblyPosition = position.Sample();
            var screwPosition = position.Sample();
            float assemblyDiameter = 0.03f*1.73f;

            if(screwPosition.z >= assemblyPosition.z - assemblyDiameter){
                if(screwPosition.x >= assemblyPosition.x - assemblyDiameter && screwPosition.x <= assemblyPosition.x + assemblyDiameter){
                    if(screwPosition.y >= assemblyPosition.y - assemblyDiameter && screwPosition.y <= assemblyPosition.y + assemblyDiameter){
                        screwPosition.z = assemblyPosition.z - assemblyDiameter;
                    }
                }
            }

            foreach (var tag in tags){
                var eulerAngles = rotation.Sample();
                tag.transform.rotation = Quaternion.Euler(eulerAngles);
                if(tag.name == "Bolt"){
                    tag.transform.position = screwPosition;
                }
                else{
                    tag.transform.position = assemblyPosition;
                }
                
            }
        }
    }
}
