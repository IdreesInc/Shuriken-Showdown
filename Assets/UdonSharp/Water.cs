using UdonSharp;
using UnityEngine;
public class Water : UdonSharpBehaviour
{
    private readonly float BOB_HEIGHT = 0.25f;
    private readonly float BOB_SPEED = 1.0f;
    private readonly float SCALE_BOB_AMOUNT = 2f; // Amount to oscillate scale
    private readonly float SCALE_BOB_BASE = 4f;   // Base scale

    private Vector3 startPosition;
    private Vector3 startScale;
    private float timeOffset;
    
    void Start()
    {
        startPosition = transform.position;
        startScale = transform.localScale;
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }
    
    void Update()
    {
        float bob = Mathf.Sin((Time.time * BOB_SPEED) + timeOffset);
        float newY = startPosition.y + bob * BOB_HEIGHT;
        float newScaleY = SCALE_BOB_BASE + bob * SCALE_BOB_AMOUNT;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        transform.localScale = new Vector3(startScale.x, newScaleY, startScale.z);
    }
}
