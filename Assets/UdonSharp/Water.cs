using UdonSharp;
using UnityEngine;
public class Water : UdonSharpBehaviour
{
    private readonly float BOB_HEIGHT = 0.25f;
    private readonly float BOB_SPEED = 1.0f;

    private Vector3 startPosition;
    private float timeOffset;
    
    void Start()
    {
        startPosition = transform.position;
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }
    
    void Update()
    {
        float newY = startPosition.y + Mathf.Sin((Time.time * BOB_SPEED) + timeOffset) * BOB_HEIGHT;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
}
