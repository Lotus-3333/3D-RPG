using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Golem : EnemyController
{
    [Header("Skill")]
    public float kickForce = 40;
    public GameObject rockPrefab;
    public Transform handPos;

    // Animation Event
    public void KickOff()
    {
        if (attackTarget != null && transform.IsFacingTarget(attackTarget.transform))
        {
            var targetStats = attackTarget.GetComponent<CharacterStats>();

            Vector3 direction = (attackTarget.transform.position - transform.position).normalized;
            // direction.Normalize();

            targetStats.GetComponent<NavMeshAgent>().isStopped = true;
            targetStats.GetComponent<NavMeshAgent>().velocity = direction * kickForce;
            // 根据个人喜好添加
            targetStats.GetComponent<Animator>().SetTrigger("Dizzy");
            targetStats.TakeDamage(characterStats, targetStats);
        }
    }

    // Animation Event
    public void ThrowRock()
    {
        if (attackTarget == null)
            attackTarget = FindObjectOfType<PlayerController>().gameObject;
        if (attackTarget != null)
        {
            var rock = Instantiate(rockPrefab, handPos.position, Quaternion.identity);
            rock.GetComponent<Rock>().target = attackTarget;
        }
    }
}
