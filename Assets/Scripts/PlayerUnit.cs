using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUnit : Unit
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isPlayerControlled = true;
    }
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, 5.0f * Time.deltaTime);
    }

    public void Die() {
        Debug.Log("The game should end now.");
        manager.unitRanks[rank][file] = null;
        alive = false;
        transform.position = new Vector3(-100, -100, 0);
        targetPosition = new Vector3(-100, -100, 0);
        StartCoroutine(GameOver());
    }

    IEnumerator GameOver() {
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene(sceneName:"WinScene");
    }
}
