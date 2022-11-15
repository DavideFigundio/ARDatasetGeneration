using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

[Serializable]
[AddRandomizerMenu("dfigu/Light Randomizer")]
public class MyLightRandomizer : Randomizer
{
    public FloatParameter lightIntensityParameter;

    [Tooltip("The range of random rotations to assign to target light sources [°].")]
    public Vector3Parameter rotation = new Vector3Parameter
    {
        x = new UniformSampler(0, 360),
        y = new UniformSampler(0, 360),
        z = new UniformSampler(0, 360)
    };

    protected override void OnIterationStart()
    {   
        var tags = tagManager.Query<LightRandomizerTag>();

        foreach (var tag in tags){
            tag.transform.rotation = Quaternion.Euler(rotation.Sample());
            var light = tag.GetComponent<Light>();            
            light.intensity = lightIntensityParameter.Sample();
        }
    }
}
