﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerSwitching : MonoBehaviour
{

    private int
        totalPlayers,
        remainingPlayers;
    public int
        currentIndex = 0,
        nextIndex;
    public float timer { get; set; }
    public float turnTime { get; set; }
    private bool skipTurn;
    public bool
        playerWin,
        startingGame,
        DEBUG_MODE,
        passingController;
    private bool[] isOut;

    [SerializeField]
    private GameObject InControl;
    public GameObject[] spawnPoints;
    public HUDManager hudManager;

    private void Awake()
    {
        if(Application.isEditor && DataManager.CurrentGameMode == DataManager.GameMode.None)
        {
            DEBUG_MODE = true;
        }
        if (DEBUG_MODE)
        {
            Instantiate(InControl);
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;
        turnTime = DataManager.TurnTime;
        totalPlayers = DataManager.TotalPlayers;
        remainingPlayers = totalPlayers;
        isOut = new bool[remainingPlayers];
        for (int i = 0; i < totalPlayers; i++)
        {
            isOut[i] = false;
        }
        // Start the timer
        timer = turnTime;
        if (!DEBUG_MODE)
        {
            StartCoroutine(StartingCountdown());
        }
        StartCoroutine(Vibrate(currentIndex));
        // hudManager.DisplayNextPlayer(currentIndex + 1);
    }

    private void Update()
    {
        if (!DEBUG_MODE)
        {
            if (!playerWin && !startingGame)
            {
                if (timer > 0)
                {
                    timer -= Time.deltaTime;
                }
                else
                {
                    SwitchPlayer();
                }
                if (passingController)
                {
                    StartCoroutine(Sleep(DataManager.PotatoDelay));
                }
            }
        }
    }

    private void SwitchPlayer()
    {
        // set the current index from the next index var
        currentIndex = NextPlayer();

        string notif = DataManager.GetPlayerIdentifier(currentIndex) + " IS UP";

        hudManager.EnqueueAction(hudManager.DisplayNotificationText(notif));
        hudManager.EnqueueWait(2f);
        hudManager.EnqueueAction(hudManager.DisplayNotificationText(""));

        if (DataManager.CurrentGameMode == DataManager.GameMode.HotPotato)
        {
            StartCoroutine(Sleep(DataManager.PotatoDelay));
        } else {
            StartCoroutine(Sleep(DataManager.PartyDelay));
        }
        hudManager.UpdateLivesDisplay();
        timer = turnTime;
        GameObject.FindGameObjectWithTag("Player").GetComponent<CarControl>().shield = false;
        StartCoroutine(Vibrate(currentIndex));
        skipTurn = false;
    }

    public int NextPlayer()
    {
        int index = (skipTurn) ? currentIndex + 1 : currentIndex;
        index++;
        if (index >= totalPlayers) index = 0;
        for (int i = index; i < totalPlayers; i++)
        {
            if (!isOut[index]) { return index; }
        }
        return -1;
    }

    public void SkipPlayer()
    {
        skipTurn = true;
    }

    public void RemovePlayer()
    {
        isOut[currentIndex] = true;
        remainingPlayers--;

        if (remainingPlayers > 1) {
            string notifText1 = DataManager.GetPlayerIdentifier(currentIndex) + " ELIMINATED!";
            string notifText2 = "PLAYERS LEFT: " + remainingPlayers;
            StartCoroutine(hudManager.DisplayOverlayText(notifText1));
            hudManager.EnqueueWait(1f);
            hudManager.EnqueueAction(hudManager.DisplayOverlayText(""));
            hudManager.EnqueueAction(hudManager.DisplayNotificationText(notifText2));
            hudManager.EnqueueWait(2f);
            hudManager.EnqueueAction(hudManager.DisplayNotificationText(""));

            SwitchPlayer();
        }

        if (remainingPlayers <= 1)
        {
            playerWin = true;
            for (int i = 0; i < totalPlayers; i++)
            {
                if (isOut[i] == false)
                {
                    string gameover = DataManager.GetPlayerIdentifier(i) + " WINS!";
                    StartCoroutine(hudManager.DisplayOverlayText(gameover));
                    hudManager.EnqueueWait(1f);
                    hudManager.EnqueueAction(hudManager.DisplayPostGameMenu());
                    break;
                }
            }
        }
    }

    private IEnumerator Vibrate(int index)
    {
        DataManager.PlayerList[index].Controller.Vibrate(100f);
        yield return new WaitForSeconds(0.25f);
        DataManager.PlayerList[index].Controller.StopVibration();
    }

    private IEnumerator StartingCountdown() {
        startingGame = true;
        Rigidbody carRigid = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody>();
        carRigid.constraints = RigidbodyConstraints.FreezeAll;

        float countdown = 3f;
        while (countdown > 0) {
            countdown -= Time.deltaTime;
            float roundedTimer = Mathf.Round(countdown);
            StartCoroutine(hudManager.DisplayOverlayText(roundedTimer + ""));
            if (roundedTimer == 0) {
                StartCoroutine(hudManager.DisplayOverlayText("BEND YOUR FRIENDS!"));
            }
            Debug.Log(roundedTimer);
            yield return null;
        }
        startingGame = false;
        carRigid.constraints = RigidbodyConstraints.None;
        yield return new WaitForSeconds(1f);
        StartCoroutine(hudManager.DisplayOverlayText(""));
    }

    private IEnumerator Sleep(float wait) {
        Time.timeScale = 0.000001f;
        yield return new WaitForSeconds(wait * 0.000001f);
        passingController = false;
        Time.timeScale = 1f;
    }
}
