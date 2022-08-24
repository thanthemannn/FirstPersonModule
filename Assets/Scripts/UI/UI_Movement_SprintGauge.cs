using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Than.Physics3D;

public class UI_Movement_SprintGauge : MonoBehaviour
{
    public Movement movementSource;
    public Image image;
    public CanvasGroup canvasGroup;

    public Color color_normal = Color.green;
    public Color color_sprint = Color.green;
    public Color color_cooldown = Color.yellow;
    public Color color_recharge = Color.green;


    void OnEnable()
    {
        movementSource.onSprintStateChanged += TrySprintUI;
        TrySprintUI(movementSource.sprinting);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        canvasGroup.alpha = 0;
        coroutineRunning = false;
        movementSource.onSprintStateChanged -= TrySprintUI;
    }

    private void TrySprintUI(bool sprint)
    {
        if (!movementSource.IsSprintInfinite && movementSource.SprintRemainingPercent < 1)
        {
            if (!coroutineRunning)
            {
                coroutineRunning = true;
                StopAllCoroutines();
                StartCoroutine(SprintUICoroutine());
            }
        }
        else
            canvasGroup.alpha = 0;
    }

    bool coroutineRunning = false;
    IEnumerator SprintUICoroutine()
    {
        coroutineRunning = true;
        canvasGroup.alpha = 1;

        float sprintPercent;
        do
        {
            sprintPercent = movementSource.SprintRemainingPercent;
            RefreshDisplay(sprintPercent, movementSource.current_sprintState);

            yield return null;
        }
        while (sprintPercent < 1);

        canvasGroup.alpha = 0;
        coroutineRunning = false;
    }

    void RefreshDisplay(float sprintPercent, Movement.SprintState state)
    {
        image.fillAmount = sprintPercent;

        Color c = color_normal;
        if (state == Movement.SprintState.sprint)
        {
            c = color_sprint;
        }
        else if (state == Movement.SprintState.cooldown)
        {
            c = color_cooldown;
        }
        else if (state == Movement.SprintState.recharge)
        {
            c = color_recharge;
        }

        image.color = c;
        canvasGroup.alpha = 1;
    }
}
