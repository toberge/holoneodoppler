using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vuforia;

[RequireComponent(typeof(Text))]
public class UmojaVersions : MonoBehaviour
{
    private void Start()
    {
        var applicationName = SceneManager.GetActiveScene().name == "HoloUmoja" ? "HoloUmoja" : "HoloNeoDoppler";
        var versions = $"<size=13><b>{applicationName}</b>: {Application.version}</size>\n" +
                       $"Unity: {Application.unityVersion}\n" +
                       $"Vuforia: {VuforiaApplication.GetVuforiaLibraryVersion()}";
        GetComponent<Text>().text = versions;
    }
}