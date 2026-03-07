using UnityEngine;

public class UIPanelManager : MonoBehaviour
{
    public GameObject[] panels;
    private void Start()
    {
        EnablePanel(0);
    }
    public void EnablePanel(string name)
    {
        foreach (GameObject obj in panels)
        {
            if (obj.name == name)
                obj.SetActive(true);
            else
                obj.SetActive(false);
        }
    }

    public void EnablePanel(int index)
    {
        foreach (GameObject obj in panels)
            obj.SetActive(false);
        panels[index].SetActive(true);
    }
}
