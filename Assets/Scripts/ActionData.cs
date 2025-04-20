using UnityEditor;
using UnityEngine;

public class ActionData : MonoBehaviour
{
    public string actionName;
    public TargetStyles targetStyle;
    public int range; // max reach for melee, targets pierced for ranged
    public Unit user;
    public int damageMod = 0;
    public int secondary = 0;
    public Sprite iconSprite;
    public bool aimLeft;
    public bool aimRight;
    [TextArea(5,100)]
    public string actionTooltip;

    public enum TargetStyles {
        self,
        melee,
        ranged,
        omni,
        hackAway,
    }

    public bool[] CalculateLegalTargets() {
        bool[] results = new bool[8];
        if (targetStyle == TargetStyles.self) {
            results[user.rank] = true;

        } else if (targetStyle == TargetStyles.melee) {
            for (int i = 1; i <= range; i++) {
                if (user.rank + i <= 7 && aimRight) {
                    results[user.rank + i] = true;
                }
                if (user.rank - i >= 0 && aimLeft) {
                    results[user.rank - i] = true;
                }
            }

        } else if (targetStyle == TargetStyles.ranged) {
            int temp = range;
            if (aimRight) {
                for (int i = user.rank + 1; i <= 7; i++) {
                    if (user.manager.CheckRankAllegiance(i) != 0 && !(user.manager.unitRanks[i][0] == user && user.manager.unitRanks[i][1] == null)) {
                        temp--;
                        if (temp == 0) {
                            results[i] = true; // temp
                        }
                    }
                }
            }
            if (aimLeft) {
                for (int i = user.rank - 1; i >= 0; i--) {
                    if (user.manager.CheckRankAllegiance(i) != 0 && !(user.manager.unitRanks[i][0] == user && user.manager.unitRanks[i][1] == null)) {
                        temp--;
                        if (temp == 0) {
                            results[i] = true; // temp
                        }
                    }
                }
            }

        } else if (targetStyle == TargetStyles.hackAway) {
            if (user.rank+1 < 8 && user.manager.CheckRankAllegiance(user.rank+1) != 0) {
                results[user.rank+1] = true;
            } else if (user.rank+2 < 8) {
                results[user.rank+2] = true;
            }
        } else {
            for (int i = 0; i < 8; i++) {
                results[i] = true;
            }
        }
        return results;
    }

    public void PlaySound() {
        GetComponent<AudioSource>().Play();
    }
}
