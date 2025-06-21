using UnityEngine;

public class CommanderClick : MonoBehaviour
{
    private CommanderSkill skill;

    void Start()
    {
        skill = GetComponent<CommanderSkill>();
    }

    void OnMouseUp()
    {
        if (skill != null && skill.CanUseSkill())
        {
            skill.UseSkill();
            Debug.Log("Skill digunakan dari komander visual!");
        }
        else
        {
            Debug.Log("Skill masih cooldown.");
        }
    }
}
