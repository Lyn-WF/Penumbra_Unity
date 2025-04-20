using System;
using Mono.Cecil;
using UnityEngine;

public class EnemyUnit : Unit
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int movePlan;
    public ActionData actionTypePlan;
    public int actionAimPlan;
    public Unit[] idealMeatShields;
    public int idealDistance = 1;
    public Unit[] players;
    public Callout callout;
    public Callout movementCallout;
    public bool activeState = true;


    void Start()
    {
        isPlayerControlled = false;
    }
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, 5.0f * Time.deltaTime);
    }

    public void TakeTurn() {
        if (!activeState) {
            Debug.LogError("Enemy was prompted for a second turn.");
            return;
        }
        if (movePlan != 0) {
            TakeAction(0, rank+movePlan);
        }
        if (actionTypePlan != null) {
            movePlan = 0;
            UpdateActionTarget();
            movementCallout.hideCallout();

            bool result = TakeAction(Array.IndexOf(actions, actionTypePlan)+1, actionAimPlan);
            if (!result) {
                manager.createNewToast("Action Failed!", Color.red, rank, 0);
            }
        }
        callout.hideCallout();
        activeState = false;
    }

    public void UpdateActionTarget() {
        if (!activeState || !actionTypePlan || !alive) {
            return;
        }
        var tempRankHolder = rank;
        rank += movePlan;
        if (actionTypePlan.targetStyle == ActionData.TargetStyles.ranged) { // Recalculate ranged attack target to accomodate movements
            bool[] temp = actionTypePlan.CalculateLegalTargets();
            for (int i = 0; i < 7; i++) {
                if (temp[i]) {
                    actionAimPlan = i;
                    break;
                }
            }
        }
        if (actionTypePlan.targetStyle == ActionData.TargetStyles.melee) {
            if (rank == 0) { // would target rank -1 and crash the game, deeply implausible for this to happen bc they'd have to get past Serra and Ingrid, but let's be safe.
                manager.createNewToast("Action Failed!", Color.red, rank, file);
                actionTypePlan = null;
                return;
            }
            actionAimPlan = rank - 1;
        }
        rank = tempRankHolder;
        callout.showCallout(-7.8f + (16.0f/7.0f*actionAimPlan), -8.0f + (16.0f/7.0f*(movePlan+rank)));
        if (movePlan != 0) {
            movementCallout.showCallout(-8.0f + (16.0f/7.0f*(movePlan+rank)), -8.0f + (16.0f/7.0f*rank));
        }
    }

    public void PlanTurn() {
        bool foundTarget = false;
        movePlan = 0;
        activeState = true;
        if (!alive) {
            return;
        }
        
        foreach (var target in idealMeatShields) { // Check for a target
            if (target.alive) {
                movePlan = Math.Clamp(target.rank + idealDistance - rank, -2, 2);
                foundTarget = true;
                break;
            }
        }

        if (rank-1 >= 0 && manager.CheckRankAllegiance(rank-1) == 0 && !foundTarget) { // Otherwise we just treat the foremost player unit as the target
            movePlan = -1;
            if (rank-2 >= 0 && manager.CheckRankAllegiance(rank-2) == 0) {
                movePlan = -2;
            }
        }

        if (movePlan == 0) {
            movementCallout.hideCallout();
        } else {
            movementCallout.showCallout(-8.0f + (16.0f/7.0f*(movePlan+rank)), -8.0f + (16.0f/7.0f*rank));
        }
        //Debug.Log(name + " is planning to move "+movePlan+" spaces");

        var tempRankHolder = rank;
        rank += movePlan;
        actionTypePlan = null;
        foreach (var action in actions) {
            if (action.secondary == 5 && hp >= maxHp/2) {
                continue; // Do not consider using Life Tap if HP > 50%
            }
            bool[] possibleTargets = action.CalculateLegalTargets(); // Used to determine if an attack should be cued but is NOT the final target, will be recalced on launch unless attack omnitargets
            for (int i = 0; i < 8; i++) {
                if (possibleTargets[i]) {
                    foreach(var playerUnit in players) {
                        if (playerUnit.rank == i && (playerUnit.hp <= action.damageMod + atk || actionTypePlan == null)) {
                            actionTypePlan = action;
                            actionAimPlan = i;
                            if (playerUnit.hp <= action.damageMod + atk) { // Makes sure first player index (Serra) is still prioritized with multiple kill options, but one kill overrides a higher indexed non-kill
                                rank = tempRankHolder;
                                if (actionTypePlan != null) {
                                    callout.showCallout(-7.8f + (16.0f/7.0f*actionAimPlan), -8.0f + (16.0f/7.0f*(movePlan+rank)));
                                    Debug.Log(name + " is planning to use "+actionTypePlan.actionName+" on rank "+actionAimPlan);
                                } else {
                                    Debug.Log(name + " is not planning an action");
                                }
                                return;
                            }
                        }
                    }
                }
            }
        }
        rank = tempRankHolder;
        if (actionTypePlan != null) {
            callout.showCallout(-7.8f + (16.0f/7.0f*actionAimPlan), -8.0f + (16.0f/7.0f*(movePlan+rank)));
            Debug.Log(name + " is planning to use "+actionTypePlan.actionName+" on rank "+actionAimPlan);
        } else {
            Debug.Log(name + " is not planning an action");
        }
    }

    public void Die() {
        Debug.Log("An enemy unit died.");
        manager.unitRanks[rank][file] = null;
        if (file == 0) {
            manager.unitRanks[rank][0] = manager.unitRanks[rank][1];
            manager.unitRanks[rank][1] = null;
        }
        if (file != 2) {
            manager.unitRanks[rank][1] = manager.unitRanks[rank][2];
            manager.unitRanks[rank][2] = null;
        }
        alive = false;
        callout.hideCallout();
        movementCallout.hideCallout();
        manager.CheckGameWon();
        gameObject.SetActive(false);
    }
}
