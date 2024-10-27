
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Effects : UdonSharpBehaviour {

    public GameObject explosion;

    public static Effects Get() {
        return GameObject.Find("Local Effects").GetComponent<Effects>();
    }

    public void SpawnExplosion( Vector3 position, int level = 0) {
        GameObject clone = Instantiate(explosion);
        clone.transform.position = position;
        foreach (ParticleSystem ps in clone.GetComponentsInChildren<ParticleSystem>()) {
            ps.Play();
        }

        if (level > 0) {
            int numOfClones = 3 * level;
            float randomRange = 0.5f * level;
            for (int i = 0; i < numOfClones; i++) {
                SpawnExplosion(position + new Vector3(UnityEngine.Random.Range(-randomRange, randomRange), UnityEngine.Random.Range(-randomRange, randomRange), UnityEngine.Random.Range(-randomRange, randomRange)));
            }
        }
    }
}
