using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TestBoss : BossController
{

    [Header("Animation Clips")]
    [SerializeField] private AnimationClip appearAnimation;
    [SerializeField] private AnimationClip disappearAnimation;
    [SerializeField] private AnimationClip attackPreparationAnimation;
    [SerializeField] private AnimationClip attackAnimation;
    [SerializeField] private AnimationClip specialAnimation;
    [SerializeField] private AnimationClip idleAnimation;
    [SerializeField] private AnimationClip moveAnimation;
    [SerializeField] private AnimationClip deadAnimation;

    [Header("Animation Timing")]
    [SerializeField] private float appearDelay = 0f;
    [SerializeField] private float disappearDelay = 0f;
    [SerializeField] private float attackPreparationDelay = 0f;
    [SerializeField] private float attackDelay = 0f;
    [SerializeField] private float specialDelay = 0f;
    [SerializeField] private float deadDelay = 0f;

    public bool IsAnimationPlaying = false;

    [Header("Attack Prefabs")]
    [SerializeField] private GameObject AreaAttackPrefab;
    [SerializeField] private GameObject cancelableAttackPrefab;

    [Header("Audio System")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip appearSound;
    [SerializeField] private AudioClip disappearSound;
    [SerializeField] private AudioClip attackPreparationSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip specialSound;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip deadSound;
    [SerializeField] private AudioClip areaAttackSound;
    [SerializeField] private AudioClip cancelableAttackSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;


    protected override void MakeAIDecision()
    {
        

    }

    protected override void Start()
    {
        base.Start(); // 确保调用基类的Start方法
        
        // 如果没有设置AudioSource，尝试获取
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // 初始化状态机并设置初始状态
        InitializeStateMachine();
        stateMachine.ChangeState(new TestBossAppearState());
    }

    // 动画播放方法
    public void PlayAppearAnimation()
    {
        if (bossAnimator == null || appearAnimation == null) return;

        StartCoroutine(PlayAnimationWithDelay(appearAnimation, appearDelay));
    }

    public void PlayDisappearAnimation()
    {
        if (bossAnimator == null || disappearAnimation == null) return;

        StartCoroutine(PlayAnimationWithDelay(disappearAnimation, disappearDelay));

        Debug.Log("PlayDisappearAnimation");
    }

    public void PlayAttackPreparationAnimation()
    {
        if (bossAnimator == null || attackPreparationAnimation == null) return;

        StartCoroutine(PlayAnimationWithDelay(attackPreparationAnimation, attackPreparationDelay));
    }

    public void PlayAttackAnimation()
    {
        if (bossAnimator == null || attackAnimation == null) return;

        StartCoroutine(PlayAnimationWithDelay(attackAnimation, attackDelay));
    }

    public void PlaySpecialAnimation()
    {
        if (bossAnimator == null || specialAnimation == null) return;

        StartCoroutine(PlayAnimationWithDelay(specialAnimation, specialDelay));
    }

    public void PlayIdleAnimation()
    {
        if (bossAnimator == null || idleAnimation == null) return;

        // 直接播放动画片段
        bossAnimator.Play(idleAnimation.name);
        isPlayingAnimation = false;
    }

    public void PlayMoveAnimation()
    {
        if (bossAnimator == null || moveAnimation == null) return;

        // 直接播放动画片段
        bossAnimator.Play(moveAnimation.name);
        isPlayingAnimation = false;
    }

    public override void PlayAnimation(int index)
    {
        switch (index) 
        {
            case 0:
                PlayAppearAnimation();
                break;
            case 1:
                PlayDisappearAnimation();
                break;
            case 2:
                PlayAttackPreparationAnimation();
                break;
            case 3:
                PlayAttackAnimation();
                break;
            case 4:
                PlaySpecialAnimation();
                break;
            case 5:
                PlayIdleAnimation();
                break;
            case 6:
                PlayMoveAnimation();
                break;
            default:
                break;


        }
    }


    // 带延迟的动画播放协程
    private System.Collections.IEnumerator PlayAnimationWithDelay(AnimationClip clip, float delay)
    {
        if (bossAnimator == null || clip == null) yield break;

        isPlayingAnimation = true;

        // 等待延迟时间
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // 直接播放动画片段
        bossAnimator.Play(clip.name);

        // 等待动画播放完成
        yield return new WaitForSeconds(clip.length);

        isPlayingAnimation = false;
        PlayIdleAnimation(); // 回到待机状态
    }

    // 动画事件 触发一次区域攻击
    private void AniEvent_triggerAreaAttack() 
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) 
        {
            GameObject a = Instantiate(AreaAttackPrefab, player.transform.position + new Vector3(Random.Range(-10f, 10f), 0, 0), Quaternion.identity);
            a.GetComponent<AreaAttackProjectile>().SetEnemyPosition(this.transform.position);
            
            AniEvent_PlayAreaAttackSound();
            a.GetComponent<AreaAttackProjectile>().AniEvent_SetTrajectoryStartPosition(transform.position);
            a.GetComponent<AreaAttackProjectile>().AniEvent_SetTrajectoryEndPosition(player.transform.position);
        }
    }

    // 动画事件 触发一次可消除攻击 以boss为中心随机位置处
    private void AniEvent_triggerCancelableAttack() 
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            GameObject attackPrefab = Instantiate(cancelableAttackPrefab, this.transform.position, Quaternion.identity);
            AttackProjectile attackProjectile = attackPrefab.GetComponent<AttackProjectile>();
            attackProjectile.InitializeByBoss(this.transform.position, this.transform.position);
        }
    }
    
    /// <summary>
    /// 动画事件 - 播放出现音效
    /// </summary>
    private void AniEvent_PlayAppearSound()
    {
        PlaySound(appearSound);
    }

    /// <summary>
    /// 动画事件 - 播放消失音效
    /// </summary>
    private void AniEvent_PlayDisappearSound()
    {
        PlaySound(disappearSound);
    }

    /// <summary>
    /// 动画事件 - 播放攻击准备音效
    /// </summary>
    private void AniEvent_PlayAttackPreparationSound()
    {
        PlaySound(attackPreparationSound);
    }

    /// <summary>
    /// 动画事件 - 播放攻击音效
    /// </summary>
    private void AniEvent_PlayAttackSound()
    {
        PlaySound(attackSound);
    }

    /// <summary>
    /// 动画事件 - 播放特殊技能音效
    /// </summary>
    private void AniEvent_PlaySpecialSound()
    {
        PlaySound(specialSound);
    }

    /// <summary>
    /// 动画事件 - 播放移动音效
    /// </summary>
    private void AniEvent_PlayMoveSound()
    {
        PlaySound(moveSound);
    }

    /// <summary>
    /// 动画事件 - 播放死亡音效
    /// </summary>
    private void AniEvent_PlayDeadSound()
    {
        PlaySound(deadSound);
    }

    /// <summary>
    /// 动画事件 - 播放区域攻击音效
    /// </summary>
    private void AniEvent_PlayAreaAttackSound()
    {
        PlaySound(areaAttackSound);
    }

    /// <summary>
    /// 动画事件 - 播放可取消攻击音效
    /// </summary>
    private void AniEvent_PlayCancelableAttackSound()
    {
        PlaySound(cancelableAttackSound);
    }

    /// <summary>
    /// 播放指定音效的核心方法
    /// </summary>
    /// <param name="clip">要播放的音效片段</param>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.volume = soundVolume;
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// 停止当前播放的音效
    /// </summary>
    public void StopSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// 设置音效音量
    /// </summary>
    /// <param name="volume">音量值 (0-1)</param>
    public void SetSoundVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = soundVolume;
        }
    }

    protected override System.Collections.IEnumerator DeathSequence()
    {
        // 播放死亡动画
        PlayDeathAnimation();

        stateMachine.ChangeState(new TestBossDisappearState());

        while (isPlayingAnimation)
        {
            yield return null;
        }

        // 死亡效果
        yield return new WaitForSeconds(10f);
        Destroy(gameObject);
    }

}
