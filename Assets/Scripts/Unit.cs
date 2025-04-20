using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public CombatManager manager;
    public bool isPlayerControlled;
    public int maxHp = 12;
    public int hp = 12;
    public int atk = 5;
    public int buffTurns = 0;
    public Vector3 targetPosition;
    public int rank;
    public int file;
    public ActionData[] actions;
    public Sprite icon;
    public bool alive = true;
    public bool riposte = false;
    public Sprite[] reactions;

    public void updateTargetPosition(int newRank, int newFile) {
        float x = -8.0f + (16.0f/7.0f*newRank) + (newRank > 3 ? 0.1f*newFile : -0.1f*newFile);
        float y = -0.5f + 0.4f * newFile;
        float z = newFile; // send sprites behind other sprites back
        targetPosition = new Vector3(x, y, z);
        rank = newRank;
        file = newFile;
    }

    public void TakeDamage(int amount) {
        if (riposte) {
            riposte = false;
            manager.currentTurnOrder[manager.turnIndex].TakeDamage(amount);
            manager.createNewToast("Riposted:", Color.blue, rank, 1);
            manager.createNewToast(amount+"!", Color.red, manager.currentTurnOrder[manager.turnIndex].rank, 0);
            GetComponentInChildren<SpriteRenderer>().sprite = reactions[5];
            StartCoroutine(ReturnToIdle());
        } else {
            GetComponentInChildren<SpriteRenderer>().sprite = reactions[1];
            StartCoroutine(ReturnToIdle());
            hp -= amount;
            if (hp <= 0) {
                Die();
            }
        }
    }

    public void TakeHealing(int amount) {
        if (hp + amount > maxHp) {
            hp = maxHp;
        } else {
            hp += amount;
        }
    }

    public void ChangeBuffTurns(int turns) {
        if (buffTurns+turns >= 0) {
            buffTurns += turns;
            if (buffTurns == 0) {
                atk += 4;
            } else if (buffTurns - turns == 0) {
                atk += 4;
            }
        }
    }

    public bool TakeAction(int type, int targetRank) {
        if (type == 0) {
            return TakeMoveAction(targetRank);
        } else if (type <= actions.Length) {
            riposte = false;
            return TakeAttackAction(type-1, targetRank);
        } else {
            Debug.LogError("Impossible request to take action type "+type);
            return false;
        }
    }

    bool TakeAttackAction(int type, int targetRank) {
        bool[] legalTargets = actions[type].CalculateLegalTargets();
        if (legalTargets[targetRank]) {
            Debug.Log("Found a legal target rank for the "+actions[type].name+" action");
            bool result = manager.ExecuteTargetedEffect(targetRank, (actions[type].damageMod > 0 ? atk + actions[type].damageMod : actions[type].damageMod), actions[type].secondary);

            if (result) {
                if (actions[type].secondary == 4) {
                    manager.createNewToast("Riposte!", Color.blue, rank, 0);
                    riposte = true;
                } else if (actions[type].secondary == 5) {
                    TakeHealing(atk + actions[type].damageMod);
                    manager.createNewToast(atk + actions[type].damageMod+"!", Color.green, rank, -1);
                } else if (actions[type].secondary == 6) { // Lunge
                    manager.RequestUnitMove(rank, file, targetRank > rank ? rank+2 : rank-2);
                } else if (actions[type].secondary == 7)  { // Eviscerate
                    if (rank - 3 >= 0 && manager.CheckRankAllegiance(rank-3) != 2) {
                        manager.RequestUnitMove(rank, file, rank-3);
                    } else if (rank - 2 >= 0 && manager.CheckRankAllegiance(rank-2) != 2) {
                        manager.RequestUnitMove(rank, file, rank-2);
                    } else if (rank - 1 >= 0 && manager.CheckRankAllegiance(rank-1) != 2) {
                        manager.RequestUnitMove(rank, file, rank-1);
                    }
                }
                actions[type].PlaySound();
                GetComponentInChildren<SpriteRenderer>().sprite = reactions[type+2];
                StartCoroutine(ReturnToIdle());
            }
            return result;
        }
        Debug.Log("Failed to find a legal target rank for the "+actions[type].name+" action");
        return false;
    }

    bool TakeMoveAction(int targetRank) {
        int dist = targetRank - rank;
        bool moveResult;
        if (dist < -2 || dist > 2 || rank+dist < 0 || rank+dist > 7) {
            return false;
        }

        if (dist == 0) {
            return false; // move of length 0 "fails" so that it doesn't burn player action econ on a misinput
        } else if (dist > 0) {
            moveResult = manager.RequestUnitMove(rank, file, rank + 1);
        } else {
            moveResult = manager.RequestUnitMove(rank, file, rank - 1);
        }

        if (!moveResult || dist== -1 || dist == 1) {
            return moveResult;
        }

        if (dist > 0) {
            manager.RequestUnitMove(rank, file, rank + 1);
        } else {
            manager.RequestUnitMove(rank, file, rank - 1);
        }
        return true;
    }
    IEnumerator ReturnToIdle() {
        yield return new WaitForSeconds(0.5f);
        GetComponentInChildren<SpriteRenderer>().sprite = reactions[0];
    }

    void Die() {
        if (isPlayerControlled) {
            ((PlayerUnit)this).Die();
        } else {
            ((EnemyUnit)this).Die();
        }
    }
}
