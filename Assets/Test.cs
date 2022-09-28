using UnityEngine;

public class Test : MonoBehaviour
{
    private float time;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 30;
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        if (time > 1)
        {
            Debug.Log($"AIM ALIVE");
            time = 0;
        }
    }
}