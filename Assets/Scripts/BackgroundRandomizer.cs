using System;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers.Tags;
using UnityEngine.Perception.Randomization.Samplers;

// Randomizer for loading background images.

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
{
    [Serializable]
    [AddRandomizerMenu("dfigu/Background Randomizer")]
    public class BackgroundRandomizer : Randomizer
    {
        private int iterationNumber = 0;
        private int currentImage = 0;

        [Tooltip("The number of background images provided.")]
        public int backroundImageNumber;

        [Tooltip("The number of total iterations in the scenario.")]
        public int scenarioIterationNumber;

        protected override void OnIterationStart(){

            // Getting tags
            var tags = tagManager.Query<BackgroundRandomizerTag>();

            // Getting image number required for current iteration
            var imageNumber = (int)System.Math.Floor((double)iterationNumber*backroundImageNumber/scenarioIterationNumber);

            // Updating current image if required
            if(imageNumber != currentImage){
                var imgname = "azure/" + imageNumber.ToString();
                currentImage = imageNumber;
                
                var sprite = Resources.Load<Sprite>(imgname);

                foreach(var tag in tags){
                    var renderer = tag.GetComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                } 
            }
                

            iterationNumber++;
        }
    }
}
