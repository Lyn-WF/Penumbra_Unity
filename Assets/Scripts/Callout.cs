using UnityEngine;
using UnityEngine.Rendering;

public class Callout : MonoBehaviour
{
    public LineRenderer line;

    void Start()
    {
        line = GetComponentInChildren<LineRenderer>();
        line.material.SetColor("_Color", new Color(1f, 1f, 1f, 0.3f));
    }

    public void showCallout(float head, float tail) {
        gameObject.SetActive(true);
        transform.position = new Vector3(head, transform.position.y, 0);
        line.SetPosition(0, transform.position + new Vector3(head < tail ? 0.7f : -0.7f,0,0));
        line.SetPosition(1, new Vector3(tail, transform.position.y, 0));
    }

    public void hideCallout() {
        gameObject.SetActive(false);
    }
}
