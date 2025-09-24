
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Effects : UdonSharpBehaviour
{

    public GameObject explosion;

    public static Effects Get()
    {
        return GameObject.Find("Local Effects").GetComponent<Effects>();
    }

    public void SpawnExplosion(Vector3 position, int level = 0)
    {
        GameObject clone = Instantiate(explosion);
        clone.transform.position = position;
        // Set start size based on level
        float startSize = Shuriken.GetExplosionRange(level) + 0.75f;
        ParticleSystem.MainModule main = clone.GetComponent<ParticleSystem>().main;
        main.startSize = startSize;
        foreach (ParticleSystem ps in clone.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Play();
        }
    }
}
