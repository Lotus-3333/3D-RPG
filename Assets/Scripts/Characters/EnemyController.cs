using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyStates { GUARD, PATROL, CHASE, DEAD }
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CharacterStats))]
public class EnemyController : MonoBehaviour, IEndGameObserver
{
    private EnemyStates enemyStates;
    private NavMeshAgent agent;
    private Animator anim;
    private Collider coll;

    protected CharacterStats characterStats;

    [Header("Basic Settings")]
    public float sightRadius;
    public bool isGuard;
    private float speed;
    protected GameObject attackTarget;
    public float lookAtTime;
    private float remainLookAtTime;
    private float lastAttackTime;

    private Quaternion guardRotation;

    [Header("Patrol State")]
    public float patrolRange;
    private Vector3 wayPoint;
    private Vector3 guardPos;

    // bool配合动画
    bool isWalk;
    bool isChase;
    bool isFollow;
    bool isDead;
    bool playerDead;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        characterStats = GetComponent<CharacterStats>();
        coll = GetComponent<Collider>();

        speed = agent.speed;
        guardPos = transform.position;
        guardRotation = transform.rotation;
        remainLookAtTime = lookAtTime;
    }

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        if (isGuard)
        {
            enemyStates = EnemyStates.GUARD;
        }
        else
        {
            enemyStates = EnemyStates.PATROL;
            GetNewWayPoint();
        }
        // FIXME:场景切换后修改掉
        GameManager.Instance.AddObserver(this);
    }
    //切换场景时启用
    /* /// <summary>
    /// This function is called when the object becomes enabled and active.
    /// </summary>
    void OnEnable()
    {
        GameManager.Instance.AddObserver(this);
    } */

    /// <summary>
    /// This function is called when the behaviour becomes disabled or inactive.
    /// </summary>
    void OnDisable()
    {
        if (!GameManager.IsInitialized)
            return;
        GameManager.Instance.RemoveObserver(this);
    }

    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    void Update()
    {
        if (characterStats.CurrentHealth == 0)
            isDead = true;

        if (!playerDead)
        {
            SwitchStates();
            SwitchAnimation();
            lastAttackTime -= Time.deltaTime;
        }
    }

    void SwitchAnimation()
    {
        anim.SetBool("Walk", isWalk);
        anim.SetBool("Chase", isChase);
        anim.SetBool("Follow", isFollow);
        anim.SetBool("Critical", characterStats.isCritical);
        anim.SetBool("Death", isDead);
    }

    void SwitchStates()
    {
        if (isDead)
            enemyStates = EnemyStates.DEAD;

        // 如果发现playder 切换到CHASE
        else if (FoundPlayer())
        {
            enemyStates = EnemyStates.CHASE;
            // Debug.Log("找到Player");
        }

        switch (enemyStates)
        {
            case EnemyStates.GUARD:
                isChase = false;


                if (transform.position != guardPos)
                {
                    isWalk = true;
                    agent.isStopped = false;
                    agent.destination = guardPos;

                    if (Vector3.SqrMagnitude(guardPos - transform.position) <= agent.stoppingDistance)
                    {
                        isWalk = false;
                        transform.rotation = Quaternion.Lerp(transform.rotation, guardRotation, 0.01f);
                    }
                }
                break;
            case EnemyStates.PATROL:
                isChase = false;
                agent.speed = speed * 0.5f;

                // 判断是否到了随机巡逻点
                if (Vector3.Distance(wayPoint, transform.position) <= agent.stoppingDistance)
                {
                    isWalk = false;
                    if (remainLookAtTime > 0)
                        remainLookAtTime -= Time.deltaTime;
                    else
                        GetNewWayPoint();
                }
                else
                {
                    isWalk = true;
                    agent.destination = wayPoint;
                }
                break;
            case EnemyStates.CHASE:
                isWalk = false;
                isChase = true;

                agent.speed = speed;

                if (!FoundPlayer())
                {
                    isFollow = false;
                    if (remainLookAtTime > 0)
                    {
                        agent.destination = transform.position;
                        remainLookAtTime -= Time.deltaTime;
                    }
                    else if (isGuard)
                        enemyStates = EnemyStates.GUARD;
                    else
                        enemyStates = EnemyStates.PATROL;
                }
                else
                {
                    isFollow = true;
                    agent.isStopped = false;
                    agent.destination = attackTarget.transform.position;
                }
                // 在攻击范围内则攻击
                if (TargetInAttackRange() || TargetInSkillRange())
                {
                    isFollow = false;
                    agent.isStopped = true;

                    if (lastAttackTime < 0)
                    {
                        lastAttackTime = characterStats.attackData.coolDown;

                        // 暴击判断
                        characterStats.isCritical = Random.value < characterStats.attackData.criticalChance;
                        // 执行攻击
                        Attack();
                    }
                }
                break;
            case EnemyStates.DEAD:
                coll.enabled = false;
                // agent.enabled = false;
                agent.radius = 0;
                Destroy(gameObject, 2f);
                break;
        }
    }

    void Attack()
    {
        transform.LookAt(attackTarget.transform);
        if (TargetInAttackRange())
        {
            // 近身攻击动画
            anim.SetTrigger("Attack");
        }
        if (TargetInSkillRange())
        {
            // 技能攻击动画
            anim.SetTrigger("Skill");
        }
    }

    bool FoundPlayer()
    {
        var colliders = Physics.OverlapSphere(transform.position, sightRadius);

        foreach (var target in colliders)
        {
            if (target.CompareTag("Player"))
            {
                attackTarget = target.gameObject;
                return true;
            }
        }
        attackTarget = null;
        return false;
    }

    bool TargetInAttackRange()
    {
        if (attackTarget != null)
            return Vector3.Distance(attackTarget.transform.position, transform.position) <= characterStats.attackData.attackRange;
        else
            return false;
    }

    bool TargetInSkillRange()
    {
        if (attackTarget != null)
            return Vector3.Distance(attackTarget.transform.position, transform.position) <= characterStats.attackData.skillRange;
        else
            return false;
    }

    void GetNewWayPoint()
    {
        remainLookAtTime = lookAtTime;

        float randomX = Random.Range(-patrolRange, patrolRange);
        float randomZ = Random.Range(-patrolRange, patrolRange);

        Vector3 randomPoint = new Vector3(guardPos.x + randomX, transform.position.y, guardPos.z + randomZ);

        NavMeshHit hit;
        wayPoint = NavMesh.SamplePosition(randomPoint, out hit, patrolRange, 1) ? hit.position : transform.position;
    }

    /// <summary>
    /// Callback to draw gizmos only if the object is selected.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sightRadius);
    }

    // Animation Event
    void Hit()
    {
        if (attackTarget != null && transform.IsFacingTarget(attackTarget.transform))
        {
            var targetStats = attackTarget.GetComponent<CharacterStats>();
            targetStats.TakeDamage(characterStats, targetStats);
        }
    }

    public void EndNotify()
    {
        // 获胜动画
        // 停止所有移动
        // 停止Agent
        anim.SetBool("Win", true);
        playerDead = true;
        isChase = false;
        isWalk = false;
        attackTarget = null;
    }
}
