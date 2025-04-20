using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Unit[][] unitRanks = new Unit[8][];
    public Unit[] initialUnitOrder;
    public Unit[] inititiveOrder;
    public Unit[] currentTurnOrder;
    public int turnIndex = 0;
    public InputHandler playerHandler;
    public GameObject toastTemplate;
    public AudioSource musicLoop;
    public AudioSource footstepSfx;
    public AudioSource missSfx;

    void Start()
    {
        for (int i = 0; i < 8; i++) {
            unitRanks[i] = new Unit[3];
        }
        unitRanks[0][0] = initialUnitOrder[0]; // Serra
        unitRanks[2][0] = initialUnitOrder[1]; // Krys
        unitRanks[3][0] = initialUnitOrder[2]; // Ingrid
        unitRanks[4][0] = initialUnitOrder[3]; // Swordie
        unitRanks[5][0] = initialUnitOrder[4]; // Ranger
        unitRanks[7][0] = initialUnitOrder[5]; // Boss
        initialUnitOrder[0].updateTargetPosition(0, 0);
        initialUnitOrder[1].updateTargetPosition(2, 0);
        initialUnitOrder[2].updateTargetPosition(3, 0);
        initialUnitOrder[3].updateTargetPosition(4, 0);
        initialUnitOrder[4].updateTargetPosition(5, 0);
        initialUnitOrder[5].updateTargetPosition(7, 0);

        nextRound();
        musicLoop.PlayDelayed(113.455f);
    }

    public void createNewToast(string toastText, Color fontColor, int newRank, int newFile) {
        var newToast = Instantiate(toastTemplate);
        newToast.GetComponent<Toast>().SetToast(toastText, fontColor, newRank, newFile);
    }

    public bool RequestUnitMove(int startRank, int startFile, int endRank)
    {    
        if (startRank < 0 || startRank > 7 || endRank < 0 || endRank > 7 || startFile < 0 || startFile > 2) {
            Debug.LogError("Requested move from "+startRank+", "+startFile+" to "+endRank+" which is out of bounds");
            return false;
        }
        
        // Perform an error check for illegal unit stacks
        if (unitRanks[startRank][startFile] == null) {
            Debug.LogError("Illegal move request made from empty position ["+startRank+", "+startFile+"]. This should be impossible");
            return false;
        }

        Unit unitToMove = unitRanks[startRank][startFile];

        // Perform a check for moving to an occupied position
        if (CheckRankAllegiance(endRank) != 0 && CheckRankAllegiance(endRank) != CheckRankAllegiance(startRank)) {
            Debug.Log("Move requested to enemy-occuped rank "+ endRank +" failed.");
            createNewToast("Movement blocked!", Color.red, startRank, 1);
            return false;
        }

        int targetFile;
        // Place unit in new position
        if (unitRanks[endRank][0] == null) {
            unitRanks[endRank][0] = unitRanks[startRank][startFile];
            unitRanks[startRank][startFile] = null;
            targetFile = 0;
        } else if (unitRanks[endRank][1] == null) {
            unitRanks[endRank][1] = unitRanks[startRank][startFile];
            unitRanks[startRank][startFile] = null;
            targetFile = 1;
        } else if (unitRanks[endRank][2] == null) {
            unitRanks[endRank][2] = unitRanks[startRank][startFile];
            unitRanks[startRank][startFile] = null;
            targetFile = 2;
        } else {
            Debug.LogError("Illegal move request made to triply occupied rank. This should be impossible.");
            return false;
        }

        // Update old position, shifting units in higher files down
        if (startFile == 0 && unitRanks[startRank][1] != null) {
            unitRanks[startRank][1].updateTargetPosition(startRank, 0);
            unitRanks[startRank][0] = unitRanks[startRank][1];
            unitRanks[startRank][1] = null;
        }
        if (startFile != 2 && unitRanks[startRank][2] != null) {
            unitRanks[startRank][2].updateTargetPosition(startRank, 1);
            unitRanks[startRank][1] = unitRanks[startRank][2];
            unitRanks[startRank][2] = null;
        }

        unitToMove.updateTargetPosition(endRank, targetFile);
        foreach(var unit in inititiveOrder) {
            if (unit.GetType() == typeof(EnemyUnit)) {
                ((EnemyUnit)unit).UpdateActionTarget();
            }
        }
        footstepSfx.Play();
        return true;
    }

    public int CheckRankAllegiance(int rank) {
        if (rank < 0 || rank > 7) {
            Debug.LogError("Checked for allegience of rank "+rank+" which is out of bounds");
            return -1;
        }

        if (unitRanks[rank][0] == null) {
            return 0;
        } else if (unitRanks[rank][0].isPlayerControlled) {
            return 1;
        } else {
            return 2;
        }
    }

    public bool ExecuteTargetedEffect(int targetRank, int damage, int secondary) {
        if (targetRank < 0 || targetRank > 7) {
            Debug.LogError("Targeted effect at rank "+targetRank+" which is out of bounds");
            return false;
        }
        
        if (CheckRankAllegiance(targetRank) == 0) {
            Debug.Log("But nobody came...");
            return false;
        }
        
        for (int i = 0; i < 3; i++) {
            if (unitRanks[targetRank][secondary == 1 || secondary == 2 ? 0 : i] != null) {
                if (damage > 0) {
                    unitRanks[targetRank][secondary == 1 || secondary == 2 ? 0 : i].TakeDamage(damage);
                    createNewToast(damage+"!", Color.red, targetRank, 0);
                } else if (damage < 0) { // Serra's heal
                    unitRanks[targetRank][i].TakeHealing(damage*-1);
                    createNewToast((damage*-1)+"!", Color.green, targetRank, 0);
                }
                

                switch (secondary) { // Handles secondary effects on the target, but not on the user.
                    case 1:
                        RequestUnitMove(targetRank, 0, targetRank + 1); // Push Right
                        break;
                    case 2:
                        RequestUnitMove(targetRank, 0, targetRank - 1); // Push Left
                        break;
                    case 3: // Buff atk
                        Debug.Log("Applied buff turn");
                        createNewToast("Damage Up!", Color.blue, targetRank, 0);
                        unitRanks[targetRank][i].ChangeBuffTurns(2);
                        break;
                }
            }
        }

        return true;
    }

    public void delayTurn(bool hard) {
        Unit temp;
        int depth = 0;

        if (turnIndex+1 == currentTurnOrder.GetLength(0)) {
            nextRound();
            return;
        }

        do {
            temp = currentTurnOrder[turnIndex+1+depth];
            currentTurnOrder[turnIndex+1+depth] = currentTurnOrder[turnIndex+depth];
            currentTurnOrder[turnIndex+depth] = temp;
            depth++;
        } while (hard && temp.isPlayerControlled && turnIndex+1+depth < currentTurnOrder.Length);

        if (currentTurnOrder[turnIndex].GetType() == typeof(PlayerUnit)) {
            playerHandler.SetupTurn((PlayerUnit)currentTurnOrder[turnIndex]);
        } else {
            if (currentTurnOrder[turnIndex].alive) {
                playerHandler.SetTurnIconPanels();
                StartCoroutine(TakeEnemyTurnDelayed((EnemyUnit)currentTurnOrder[turnIndex]));
            } else {
                nextTurn();
            }
        }
    }

    public void nextTurn() {
        //Debug.Log("Ending Turn");
        if (turnIndex+1 == currentTurnOrder.GetLength(0)) {
            nextRound();
        } else {
            turnIndex += 1;
        }
        if (currentTurnOrder[turnIndex].GetType() == typeof(PlayerUnit)) {
            playerHandler.SetupTurn((PlayerUnit)currentTurnOrder[turnIndex]);
        } else {
            if (currentTurnOrder[turnIndex].alive) {
                playerHandler.SetTurnIconPanels();
                StartCoroutine(TakeEnemyTurnDelayed((EnemyUnit)currentTurnOrder[turnIndex]));
            } else {
                nextTurn();
            }
        }
    }

    IEnumerator TakeEnemyTurnDelayed(EnemyUnit unit) {
        yield return new WaitForSeconds(1);
        unit.TakeTurn();
        yield return new WaitForSeconds(1);
        nextTurn();
    }

    void nextRound() {
        //Debug.Log("Ending Round");
        for (int i = 0; i < 6; i++) {
            currentTurnOrder[i] = inititiveOrder[i];
        }
        turnIndex = 0;
        foreach (var unit in inititiveOrder) {
            if (unit.GetType() == typeof(EnemyUnit)) {
                ((EnemyUnit)unit).PlanTurn();
            }
        }
    }

    public void CheckGameWon() {
        if (!initialUnitOrder[3].alive && !initialUnitOrder[4].alive) {
            StartCoroutine(GameOver());
        }
    }
    IEnumerator GameOver() {
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene(sceneName:"WinScene");
    }
}
