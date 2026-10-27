using UnityEngine;
using UnityEngine.SceneManagement;

public class TestArenaHelper : MonoBehaviour
{
    public Transform player;
    public SwarmController swarm;
    public Transform[] spawns;
    public GameObject helpPanel;

    private void Update()
    {
        for (int i = 0; i < spawns.Length && i < 8; i++)
            if (Input.GetKeyDown(KeyCode.F1 + i)) Teleport(spawns[i]);

        if (helpPanel != null && Input.GetKeyDown(KeyCode.H))
            helpPanel.SetActive(!helpPanel.activeSelf);

        if (Input.GetKeyDown(KeyCode.F12))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Teleport(Transform spawn)
    {
        if (player == null || spawn == null) return;
        if (swarm != null) swarm.DespawnAllImmediate();
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.SetPositionAndRotation(spawn.position, spawn.rotation);
        if (cc != null) cc.enabled = true;
    }
}
