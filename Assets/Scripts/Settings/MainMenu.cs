using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/** Main Menu controller */
public class MainMenu : MonoBehaviour
{   
    // Default values
    public static int rows = 10;
    public static int cols = 10;
    public static int colors = 6;

    [Header("UI References")]
    public TextMeshProUGUI rowsText;
    public TextMeshProUGUI colsText;
    public TextMeshProUGUI colorsText;

    void Start()
    {
        ValidateValues();
        UpdateUI();
    }

    public void PlayEvent()
    {
        SceneManager.LoadScene("Game");
    }

    /** Handle Buttons */
    public void ChangeRowCount(int amount)
    {
        rows = Mathf.Clamp(rows + amount, 2, 10);
        UpdateUI();
    }

    public void ChangeColCount(int amount) 
    {
        cols = Mathf.Clamp(cols + amount, 2, 10);
        UpdateUI();
    }

    public void ChangeColorCount(int amount)
    {
        colors = Mathf.Clamp(colors + amount, 1, 6);
        UpdateUI();
    }

    /** Private Helper Methods for value validation and printing to the UI*/
    private void ValidateValues()
    {
        rows = Mathf.Clamp(rows, 2, 10);
        cols = Mathf.Clamp(cols, 2, 10);
        colors = Mathf.Clamp(colors, 1, 6);
    }

    private void UpdateUI()
    {
        rowsText.SetText(rows.ToString());
        colsText.SetText(cols.ToString());
        colorsText.SetText(colors.ToString());
    }
}