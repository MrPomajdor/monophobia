using TMPro;
using UnityEngine;
using UnityEngine.UI;
public enum MessageBoxType
{
    Info,
    Alert,
    Error
}
public class TV_MessageBoxController : MonoBehaviour
{
    private Animator m_Animator;
    public TMP_Text Title;
    public TMP_Text Message;
    public Image typeImg;

    public Sprite InfoSprite;
    public Sprite AlertSprite;
    public Sprite ErrorSprite;

    public AudioSource aSource;
    public AudioClip ErrorClip;
    public AudioClip AlertClip;
    private void Start()
    {
        m_Animator = GetComponent<Animator>();
    }
    public void ShowMessageBox(string title, string message, MessageBoxType type)
    {
        Title.text = title;
        Message.text = message;
        switch (type)
        {
            case MessageBoxType.Info:
                typeImg.sprite = InfoSprite;
                break;
            case MessageBoxType.Alert:
                typeImg.sprite = AlertSprite;
                aSource.PlayOneShot(AlertClip);
                break;
            case MessageBoxType.Error:
                typeImg.sprite = ErrorSprite;
                aSource.PlayOneShot(ErrorClip);
                break;
        }
        m_Animator.Play("TV_MessageBoxOpen");
    }

    public void CloseMessageBox()
    {
        m_Animator.Play("TV_MessageBoxClose");
    }
}
