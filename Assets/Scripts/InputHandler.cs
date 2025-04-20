using UnityEngine;
using TMPro;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Unity.VisualScripting;

public class InputHandler : MonoBehaviour
{
    public PlayerUnit currentUnit;
    public CombatManager manager;
    public int actionSelected = -1;
    public bool canMove = true;
    public bool canAttack = true;
    public Button[] actionButtons;
    public Button[] rankButtons;
    public Image[] actionButtonIcons;
    public GameObject[] turnOrderPanels;
    public TextMeshPro text;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var btn in rankButtons) {
            btn.interactable = false;
        }
        SetActionButtonIcons();
        SetTurnIconPanels();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("1") && canAttack) {
            SelectAction(1);
        } else if (Input.GetKeyDown("2") && canAttack) {
            SelectAction(2);
        } else if (Input.GetKeyDown("3") && canAttack) {
            SelectAction(3);
        } else if (Input.GetKeyDown("4") && canAttack) {
            SelectAction(4);
        } else if (Input.GetKeyDown("5") && canMove) {
            SelectAction(5);
        } else if (Input.GetKeyDown("6") || (!canMove && !canAttack && currentUnit == manager.currentTurnOrder[manager.turnIndex])) {
            SelectAction(6);
        }

        if (actionSelected != -1) {
            if (Input.GetKeyDown("q")) {
                SelectTarget(0);
            } else if (Input.GetKeyDown("w")) {
                SelectTarget(1);
            } else if (Input.GetKeyDown("e")) {
                SelectTarget(2);
            } else if (Input.GetKeyDown("r")) {
                SelectTarget(3);
            } else if (Input.GetKeyDown("t")) {
                SelectTarget(4);
            } else if (Input.GetKeyDown("y")) {
                SelectTarget(5);
            } else if (Input.GetKeyDown("u")) {
                SelectTarget(6);
            } else if (Input.GetKeyDown("i")) {
                SelectTarget(7);
            } else if (Input.GetKeyDown(KeyCode.Escape)) {
                actionSelected = -1;
                SetRankButtonStates();
            }
        }

        actionButtons[0].interactable = canAttack && currentUnit == manager.currentTurnOrder[manager.turnIndex];
        actionButtons[1].interactable = canAttack && currentUnit == manager.currentTurnOrder[manager.turnIndex];
        actionButtons[2].interactable = canAttack && currentUnit == manager.currentTurnOrder[manager.turnIndex];
        actionButtons[3].interactable = canAttack && currentUnit == manager.currentTurnOrder[manager.turnIndex];
        actionButtons[4].interactable = canMove && currentUnit == manager.currentTurnOrder[manager.turnIndex];
        actionButtons[5].interactable = currentUnit == manager.currentTurnOrder[manager.turnIndex];
    }

    public void SetupTurn(PlayerUnit unit) {
        currentUnit = unit;
        actionSelected = -1;
        canAttack = true;
        canMove = true;
        SetRankButtonStates();
        SetActionButtonIcons();
        SetTurnIconPanels();
    }

    public void SetRankButtonStates() {
        bool[] rankTargets;
        if (actionSelected >= 1 && actionSelected <= 4) {
            rankTargets = currentUnit.actions[actionSelected-1].CalculateLegalTargets();
        } else if (actionSelected == 5) {
            rankTargets = new bool[] {false, false, false, false, false, false, false, false};
            if (currentUnit.rank > 0 && manager.CheckRankAllegiance(currentUnit.rank-1) != 2) {
                rankTargets[currentUnit.rank-1] = true;
                if (currentUnit.rank > 1 && manager.CheckRankAllegiance(currentUnit.rank-2) != 2) {
                    rankTargets[currentUnit.rank-2] = true;
                }
            }
            if (currentUnit.rank < 7 && manager.CheckRankAllegiance(currentUnit.rank+1) != 2) {
                rankTargets[currentUnit.rank+1] = true;
                if (currentUnit.rank < 6 && manager.CheckRankAllegiance(currentUnit.rank+2) != 2) {
                    rankTargets[currentUnit.rank+2] = true;
                }
            }
        } else {
            rankTargets = new bool[] {false, false, false, false, false, false, false, false};
        }


        for (int i = 0; i < 8; i++) {
            rankButtons[i].interactable = rankTargets[i];
        }
    }

    void SetActionButtonIcons() {
        for (int i = 0; i < 4; i++) {
            actionButtonIcons[i].sprite = currentUnit.actions[i].iconSprite;
        }
    }

    public void SetTurnIconPanels() {
        for (int i = 0; i < 6; i++) {
            turnOrderPanels[i].GetComponent<Image>().sprite = manager.currentTurnOrder[i].icon;
            if (i == manager.turnIndex) {
                turnOrderPanels[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(turnOrderPanels[i].GetComponent<RectTransform>().anchoredPosition.x, 145);
            } else {
                turnOrderPanels[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(turnOrderPanels[i].GetComponent<RectTransform>().anchoredPosition.x, 155);
            }
            if (manager.currentTurnOrder[i].alive) {
                turnOrderPanels[i].GetComponent<Image>().color = Color.white;
            } else {
                turnOrderPanels[i].GetComponent<Image>().color = Color.grey;
            }
        }
    }

    public void SelectAction(int index) { // Connected to action buttons
        GetComponent<AudioSource>().Play();
        if (index == 6) {
            if (canAttack && canMove) {
                manager.delayTurn(Input.GetKey(KeyCode.LeftControl));
            } else {
                manager.nextTurn();
            }
        } else if (index == actionSelected) {
            actionSelected = -1;
            actionButtons[6].Select(); // As a stupid hack there's a fake invisible button offscreen that does nothing because there's no Deselect() you have to select something new
        } else {
            actionSelected = index;
            actionButtons[index-1].Select();
        }
        SetRankButtonStates();
    }

    public void SelectTarget(int rank) { // Connected to UI Rank buttons
        GetComponent<AudioSource>().Play();
        bool result = currentUnit.TakeAction(actionSelected!=5 ? actionSelected : 0, rank);
        if (actionSelected==5) {
            canMove = !result && canMove; // False unless action failed AND could move before
        } else {
            canAttack = !result && canAttack;
        }
        actionSelected = -1;
        SetRankButtonStates();
    }

    public void setTooltipUnit(int index) {
        Unit unit = manager.currentTurnOrder[index];
        string output = unit.name + "\n";
        output += "HP: " +unit.hp+"/"+unit.maxHp+"\n";
        output += "Attack: "+(unit.name=="Krys"? (unit.atk + 2) : unit.atk)+"\n";
        text.SetText(output);

    }
    public void setTooltipAction(int action) {
        if (action == 5) {
            text.SetText("Movement:\nA unit can move up to two tiles forward or back once per turn\nUnits cannot pass through their enemies.");
        } else if (action == 6) {
            if (canAttack && canMove && manager.turnIndex != 5) {
                text.SetText("Wait:\nDelay this unit's turn until after the next one.\nHold CTRL to wait until after the next enemy turn.");
            } else {
                text.SetText("End Turn:\nSkip this unit's remaining movement and/or action.");
            }
        } else {
            text.SetText(currentUnit.actions[action-1].actionTooltip);
        }
    }

    public void setTooltipEnemyAction(EnemyUnit unit) {
        if (unit.actionTypePlan == null) {
            text.SetText("This unit is not attacking."); // Should be impossible but let's be defensive
        } else {
            text.SetText(unit.actionTypePlan.actionTooltip);
        }
    }
}


