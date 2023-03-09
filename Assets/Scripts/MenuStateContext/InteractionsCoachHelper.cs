using System;
using Microsoft.MixedReality.Toolkit.UI.HandCoach;
using UnityEngine;

public class InteractionsCoachHelper : MonoBehaviour
{
    [SerializeField] private HandInteractionHint handL;
    [SerializeField] private HandInteractionHint handR;
    [SerializeField] private HandInteractionHint probe;

    [SerializeField] private Transform handLtransform;
    [SerializeField] private Transform handRtransform;

    /// <param name="anim">NearSelect, HandFlip, AirTap, Rotate, Move, PalmUp, Scroll</param>
    public void ShowHand(Vector3 pos, string anim = "NearSelect", bool rightHand = true, Transform newParent = null)
    {
        var interaction = rightHand ? handR : handL;
        var handTransform = rightHand ? handRtransform : handLtransform;

        interaction.StopHintLoop();
        if (newParent != null)
        {
            handTransform.parent = newParent;
        }

        handTransform.position = pos;
        string handAnim = anim + (rightHand ? "_R" : "_L");
        interaction.AnimationState = handAnim;
        interaction.StartHintLoop();
    }

    public void ShowProbe(Transform pos)
    {
        probe.StopHintLoop();
        probe.transform.position = pos.position;
        probe.transform.SetParent(pos);
        probe.StartHintLoop();
    }

    public void StopHand(bool rightHand = true)
    {
        if (rightHand)
        {
            handRtransform.parent = transform;
            handR.StopHintLoop();
        }
        else
        {
            handLtransform.parent = transform;
            handL.StopHintLoop();
        }
    }

    public void StopProbe()
    {
        probe.StopHintLoop();
    }
}