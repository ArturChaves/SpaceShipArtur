using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Parallax : MonoBehaviour
{
    private float lenght;

    [Tooltip("Velocidade da camada, em unidades de mundo por segundo. " +
             "O slide pede um valor entre 0 e 1: quanto menor, mais distante a camada parece.")]
    public float parallaxEffect;

    void Start()
    {
        lenght = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void Update()
    {
        transform.position += Vector3.left * Time.deltaTime * parallaxEffect;
        if (transform.position.x < -lenght)
        {
            transform.position = new Vector3(WrapX(transform.position.x, lenght),
                                             transform.position.y,
                                             transform.position.z);
        }
    }

    public static float WrapX(float x, float lenght)
    {
        return x < -lenght ? x + lenght * 2f : x;
    }
}
