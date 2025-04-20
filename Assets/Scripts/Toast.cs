using TMPro;
using UnityEngine;

public class Toast : MonoBehaviour
{
    TextMeshPro text;
    float lifetime = 0.0f;
    Vector3 targetPosition;

    // Update is called once per frame
    void Update()
    {
        if (lifetime > 0) {
            transform.position = Vector3.Lerp(transform.position, targetPosition, 1.5f * Time.deltaTime);
            lifetime -= Time.deltaTime;
            if (lifetime <= 0.0f) {
                Destroy(gameObject);
            }
        }
    }

    public void SetToast(string toastText, Color fontColor, int newRank, int newFile) {
        text = GetComponentInChildren<TextMeshPro>();
        text.SetText(toastText);
        text.color = fontColor;
        float x = -8.0f + (2.25f*newRank);
        float y = 1 + 0.4f * newFile;
        float z = newFile; // send sprites behind other sprites back
        transform.position = new Vector3(x, y, z);
        targetPosition = new Vector3(x, y+1.5f, z);
        lifetime = 2.0f;
    }
}
