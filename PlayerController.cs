using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Steamworks;

public class PlayerController : MonoBehaviour {
    #region Feilds
    Vector2 playerVector2Position;

    [SerializeField] float speed;

    int horizontalKeyAxis;
    int verticalKeyAxis;
    [SerializeField] float horizontalForce;
    [SerializeField] float verticalForce;
    [SerializeField] float gravityForce;

    float startingFallingHeight;


    bool onGround = false;
    public bool OnGround {
        set {
            onGround = value;
        }
    }
    bool touchedSlime = false;
    public bool TouchedSlime {
        set {
            touchedSlime = value;
        }
    }

    [SerializeField] float forestHiveY;
    [SerializeField] float bossRoomY;

    bool isInForest;
    bool isInBoss;
    bool isPhase1 = true;

    bool isAttacked = false;
    public bool IsAttacked {
        set {
            isAttacked = value;
        }
    }

    bool canControl = false;
    public bool CanControl {
        get {
            return canControl;
        }
    }

    float playTime = 0;
    public float PlayTime {
        get {
            return playTime;
        }
    }

    int fallTime = 0;
    public int FallTime {
        get {
            return fallTime;
        }
    }

    int enemyAttackTime = 0;
    public int EnemyAttackTime {
        get {
            return enemyAttackTime;
        }
    }

    int bossAttackTime = 0;
    public int BossAttackTime {
        get {
            return bossAttackTime;
        }
    }
    

    Vector2 attackedVelocity;
    public Vector2 AttackedVelocity {
        set {
            attackedVelocity = value;
        }
    }

    [SerializeField] LayerMask enemyLayer;
    [SerializeField] Transform attackingPosition;
    [SerializeField] Vector2 boxSize;
    [SerializeField] float beforeDelay;
    [SerializeField] float afterDelay;

    [SerializeField] Rigidbody2D playerRigidbody;
    public Rigidbody2D PlayerRigidbody {
        get {
            return playerRigidbody;
        }
    }
    [SerializeField] Animator bodyAnimator;
    [SerializeField] Animator wingAnimator;
    [SerializeField] PlayerInput playerInput;
    [SerializeField] PlayerAudioManager playerAudioManager;

    Coroutine attackCoroutine = null;
    Coroutine emergencyCoroutine = null;

    Sound[] currentBgmSoundarray = {null, null};
    bool bgmChange = false;
    int lerpValue;
    [SerializeField] int bgmFadeFrame;

    [SerializeField] GameObject forestLevel;
    [SerializeField] GameObject hiveLevel;
    [SerializeField] BossController bossController;
    #endregion

    void Awake() {
        PlayerData playerData = SaveSystem.LoadPlayer();
        if (playerData != null) {
            gameObject.transform.position = new Vector3(playerData.positionX, playerData.positionY, 0);
            playerRigidbody.velocity = new Vector2(playerData.velocityX, playerData.velocityY);
            canControl = playerData.canControl;
            playTime = playerData.playTime;
            fallTime = playerData.fallTime;
            enemyAttackTime = playerData.enemyAttackTime;
            bossAttackTime = playerData.bossAttackTime;
        }
        else {
            bodyAnimator.SetBool("IsFalling", true);
            wingAnimator.SetBool("IsFalling", true);
            bodyAnimator.SetBool("IsPressed", true);
            wingAnimator.SetBool("IsPressed", true);
        }
    }
    void Start() {
        playerVector2Position = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);

        if (playerVector2Position.y < forestHiveY) {
            currentBgmSoundarray[0] = playerAudioManager.SearchSound("ForestBgm");
            isInForest = true;
            isInBoss = false;
            forestLevel.SetActive(true);

        }
        else if (playerVector2Position.y < bossRoomY) {
            currentBgmSoundarray[0] = playerAudioManager.SearchSound("HiveBgm");
            isInForest = false;
            isInBoss = false;
            hiveLevel.SetActive(true);
        }
        else {
            currentBgmSoundarray[0] = playerAudioManager.SearchSound("Phase1Bgm");
            isInForest = false;
            isInBoss = true;
            hiveLevel.SetActive(true);
            bossController.SetHealthBarActive(true);
        }
        PlayBgm(AudioSettings.dspTime + 0.333);
    }
    void FixedUpdate() {
        playerVector2Position = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);
        #region State
        if (onGround) {
            if (!canControl) {
                canControl = true;
                if (emergencyCoroutine != null) {
                    StopCoroutine(emergencyCoroutine);
                    emergencyCoroutine = null;
                }
                if (startingFallingHeight - playerVector2Position.y >= 2.7f) {
                    if (playerAudioManager.SearchSound("PlayerCrushed").audioSource.isPlaying) {
                        playerAudioManager.SearchSound("PlayerCrushed").audioSource.Stop();
                    }
                    playerAudioManager.SearchSound("PlayerCrushed").audioSource.Play();
                    bodyAnimator.SetBool("IsPressed", true);
                    wingAnimator.SetBool("IsPressed", true);
                    fallTime += 1;

                    if (SteamManager.Initialized) {
                        SteamUserStats.GetStat("PLAYER_FALL", out int steamFallCount);
                        steamFallCount++;
                        SteamUserStats.SetStat("PLAYER_FALL", steamFallCount);
                        SteamUserStats.StoreStats();
                    }
                }
                bodyAnimator.SetBool("IsFalling", false);
                bodyAnimator.SetBool("TouchingSlime", false);
                bodyAnimator.SetBool("TouchingHoney", false);
                wingAnimator.SetBool("IsFalling", false);
                wingAnimator.SetBool("TouchingSlime", false);
                wingAnimator.SetBool("TouchingHoney", false);
            }
            else if (attackCoroutine != null && !bodyAnimator.GetBool("OnGround")) {
                TurnIsAttackingOff();
            }
            bodyAnimator.SetBool("OnGround", true);
            wingAnimator.SetBool("OnGround", true);
        }
        else {
            if (touchedSlime) {
                if (canControl) {
                    if (playerAudioManager.SearchSound("PlayerSlimed").audioSource.isPlaying) {
                        playerAudioManager.SearchSound("PlayerSlimed").audioSource.Stop();
                    }
                    playerAudioManager.SearchSound("PlayerSlimed").audioSource.Play();
                    startingFallingHeight = playerVector2Position.y;
                    canControl = false;
                    if (attackCoroutine != null) {
                        TurnIsAttackingOff();
                    }
                    bodyAnimator.SetBool("IsFalling", true);
                    bodyAnimator.SetBool("IsMoving", false);
                    wingAnimator.SetBool("IsFalling", true);
                    wingAnimator.SetBool("IsMoving", false);
                    
                }
                if (playerVector2Position.y < forestHiveY) {
                    bodyAnimator.SetBool("TouchingHoney", false);
                    wingAnimator.SetBool("TouchingHoney", false);
                    bodyAnimator.SetBool("TouchingSlime", true);
                    wingAnimator.SetBool("TouchingSlime", true);
                }
                else {
                    bodyAnimator.SetBool("TouchingSlime", false);
                    wingAnimator.SetBool("TouchingSlime", false);
                    bodyAnimator.SetBool("TouchingHoney", true);
                    wingAnimator.SetBool("TouchingHoney", true);
                }
            }
            bodyAnimator.SetBool("OnGround", false);
            wingAnimator.SetBool("OnGround", false);
        }
        if (isAttacked) {
            if (canControl) {
                startingFallingHeight = playerVector2Position.y;
                canControl = false;
                if (attackCoroutine != null) {
                    TurnIsAttackingOff();
                }
                bodyAnimator.SetBool("IsFalling", true);
                bodyAnimator.SetBool("IsMoving", false);
                wingAnimator.SetBool("IsFalling", true);
                wingAnimator.SetBool("IsMoving", false);
            }
            playerRigidbody.velocity = attackedVelocity;
            isAttacked = false;
        }
        #endregion

        #region Control
        horizontalKeyAxis = 0;
        verticalKeyAxis = 0;

        if (playerInput.actions["Left"].IsPressed() && !playerInput.actions["Right"].IsPressed()) {
            horizontalKeyAxis = -1;
            if (attackCoroutine == null) {
                gameObject.transform.eulerAngles = new Vector3(0, 180, 0);
            }
        }
        else if (playerInput.actions["Right"].IsPressed() && !playerInput.actions["Left"].IsPressed()) {
            horizontalKeyAxis = 1;
            if (attackCoroutine == null) {
                gameObject.transform.eulerAngles = new Vector3(0, 0, 0);
            }
        }
        if (playerInput.actions["Up"].IsPressed()) {
            verticalKeyAxis = 1;
        }
        

        if (canControl) {
            if (onGround) {              
                if (attackCoroutine == null) {
                    if (playerInput.actions["Attack"].triggered) {
                        bodyAnimator.SetBool("IsAttacking", true);
                        bodyAnimator.SetBool("IsPressed", false);
                        wingAnimator.SetBool("IsPressed", false);
                        attackCoroutine = StartCoroutine(AttackLifeCycle(beforeDelay, afterDelay));
                    }
                }
                if (attackCoroutine != null) {
                    horizontalKeyAxis = 0;
                    verticalKeyAxis = 0;
                }
                playerRigidbody.velocity = new Vector2(speed * horizontalKeyAxis, playerRigidbody.velocity.y);
                if (horizontalKeyAxis == 0) {
                    bodyAnimator.SetBool("IsMoving", false);
                    wingAnimator.SetBool("IsMoving", false);
                    if (bodyAnimator.GetBool("IsPressed")) {
                        if (verticalKeyAxis != 0) {
                            bodyAnimator.SetBool("IsPressed", false);
                            wingAnimator.SetBool("IsPressed", false);
                        }
                    }
                }
                else {
                    bodyAnimator.SetBool("IsMoving", true);
                    wingAnimator.SetBool("IsMoving", true);
                    if (bodyAnimator.GetBool("IsPressed")) {
                        bodyAnimator.SetBool("IsPressed", false);
                        wingAnimator.SetBool("IsPressed", false);
                    }
                }
            }
            else {
                if (attackCoroutine == null) {
                    if (playerInput.actions["Attack"].triggered) {
                        bodyAnimator.SetBool("IsAttacking", true);
                        attackCoroutine = StartCoroutine(AttackLifeCycle(beforeDelay, afterDelay));
                    }
                }
                playerRigidbody.AddForce(Vector2.right * horizontalKeyAxis * horizontalForce);
                if (horizontalKeyAxis == 0 && verticalKeyAxis == 0) {
                    bodyAnimator.SetBool("IsMoving", false);
                    wingAnimator.SetBool("IsMoving", false);
                }
                else {
                    bodyAnimator.SetBool("IsMoving", true);
                    wingAnimator.SetBool("IsMoving", true);              
                }
            }
            playerRigidbody.AddForce(Vector2.up * verticalKeyAxis * verticalForce);
        }
        else {
            if (playerRigidbody.velocity.magnitude <= 0.001f) {
                if (emergencyCoroutine == null) {
                    emergencyCoroutine = StartCoroutine(EmergencyLifeCycle());
                }
            }
            else {
                if (emergencyCoroutine != null) {
                    StopCoroutine(emergencyCoroutine);
                    emergencyCoroutine = null;
                }
            }
        }
        playerRigidbody.AddForce(Vector2.down * gravityForce);
        #endregion

        #region BGM
        if (isInForest) {
            if (playerVector2Position.x <= -99 && playerVector2Position.y >= 19) {
                if (SteamManager.Initialized) {
                    SteamUserStats.GetAchievement("MUSHROOM", out bool hasAchieved);
                    if (!hasAchieved) {
                        SteamUserStats.SetAchievement("MUSHROOM");
                        SteamUserStats.StoreStats();
                    }
                }
            }
            else if (playerVector2Position.y >= forestHiveY) {
                bgmChange = true;
                lerpValue = bgmFadeFrame - 1;
                isInForest = false;
                forestLevel.SetActive(false);
                hiveLevel.SetActive(true);
                GameObject.FindWithTag("Respawn").GetComponent<ObjectPools>().ResetForestObjectPools();

                if (SteamManager.Initialized) {
                    SteamUserStats.GetAchievement("HIVE", out bool hasAchieved);
                    if (!hasAchieved) {
                        SteamUserStats.SetAchievement("HIVE");
                        SteamUserStats.StoreStats();
                    }
                }
            }
        }
        else if (!isInBoss) {
            if (playerVector2Position.x <= 91 && playerVector2Position.y >= 154) {
                if (SteamManager.Initialized) {
                    SteamUserStats.GetAchievement("JELLY", out bool hasAchieved);
                    if (!hasAchieved) {
                        SteamUserStats.SetAchievement("JELLY");
                        SteamUserStats.StoreStats();
                    }
                }
            }
            else if (playerVector2Position.y >= bossRoomY) {
                bgmChange = true;
                lerpValue = bgmFadeFrame - 1;
                isInBoss = true;
                bossController.StartBoss();

                if (SteamManager.Initialized) {
                    SteamUserStats.GetAchievement("PHASE_1", out bool hasAchieved);
                    if (!hasAchieved) {
                        SteamUserStats.SetAchievement("PHASE_1");
                        SteamUserStats.StoreStats();
                    }
                }
            }
        }
        else {
            if (playerVector2Position.y < bossRoomY) {
                bgmChange = true;
                lerpValue = bgmFadeFrame - 1;
                isInBoss = false;
                isPhase1 = true;
                bossController.ResetBoss();
            }
        }

        if (bgmChange) {
            if (lerpValue != -1) {
                SetBgmVolume(Mathf.Lerp(0, currentBgmSoundarray[0].audioSource.volume, (float)lerpValue / (float)(lerpValue + 1)));
                lerpValue -= 1;
            }
            else {
                bgmChange = false;
                
                SetBgmVolume(currentBgmSoundarray[0].volume);
                StopBgm();
                if (isInForest) {
                    currentBgmSoundarray[0] = playerAudioManager.SearchSound("ForestBgm");
                    currentBgmSoundarray[1] = null;
                    PlayBgm(AudioSettings.dspTime + 0.333);
                }
                else if (!isInBoss) {
                    currentBgmSoundarray[0] = playerAudioManager.SearchSound("HiveIntro");
                    currentBgmSoundarray[1] = playerAudioManager.SearchSound("HiveBgm");
                    PlayBgm(AudioSettings.dspTime + 0.333);
                }
                else if (isPhase1) {
                    currentBgmSoundarray[0] = playerAudioManager.SearchSound("Phase1Intro");
                    currentBgmSoundarray[1] = playerAudioManager.SearchSound("Phase1Bgm");
                    PlayBgm(AudioSettings.dspTime + 0.333);
                }
                else {
                    currentBgmSoundarray[0] = playerAudioManager.SearchSound("Phase2Intro");
                    currentBgmSoundarray[1] = playerAudioManager.SearchSound("Phase2Bgm");
                    PlayBgm(AudioSettings.dspTime + 0.333);
                }
            }
        }
        #endregion
    }

    void TurnIsAttackingOff() {
        bodyAnimator.SetBool("IsAttacking", false);
        StopCoroutine(attackCoroutine);
        attackCoroutine = null;
        if (playerAudioManager.SearchSound("PlayerAttacking").audioSource.isPlaying) {
            playerAudioManager.SearchSound("PlayerAttacking").audioSource.Stop();
        }
    }

    void AttackEnemy() {
        Collider2D attackedCollider = Physics2D.OverlapBox(attackingPosition.position, boxSize, 0, enemyLayer);
        if (attackedCollider != null) {
            if (attackedCollider.gameObject.CompareTag("Boss")) {
                if (bossController.HealthPoint != 0) {
                    if (playerAudioManager.SearchSound("EnemyAttacked").audioSource.isPlaying) {
                        playerAudioManager.SearchSound("EnemyAttacked").audioSource.Stop();
                    }
                    playerAudioManager.SearchSound("EnemyAttacked").audioSource.Play();
                    bossAttackTime += 1;
                    if (SteamManager.Initialized) {
                        SteamUserStats.GetStat("ATTACK_BOSS", out int steamBossCount);
                        steamBossCount++;
                        SteamUserStats.SetStat("ATTACK_BOSS", steamBossCount);
                        SteamUserStats.StoreStats();
                    }
                    if (bossController.HealthPoint == 1) {
                        if (!isPhase1) {
                            SavePlayer();
                        }
                        isPhase1 = false;
                        bgmChange = true;
                        lerpValue = bgmFadeFrame - 1;
                        playerAudioManager.SearchSound("BossCracked").audioSource.PlayDelayed(0.5f);

                        if (SteamManager.Initialized) {
                            SteamUserStats.GetAchievement("PHASE_2", out bool hasAchieved);
                            if (!hasAchieved) {
                                SteamUserStats.SetAchievement("PHASE_2");
                                SteamUserStats.StoreStats();
                            }
                        }
                    }
                    bossController.AttackedByPlayer();
                }
            }
            else {
                if (attackedCollider.gameObject.GetComponent<EnemyController>().HealthPoint != 0) {
                    if (playerAudioManager.SearchSound("EnemyAttacked").audioSource.isPlaying) {
                        playerAudioManager.SearchSound("EnemyAttacked").audioSource.Stop();
                    }
                    playerAudioManager.SearchSound("EnemyAttacked").audioSource.Play();
                    enemyAttackTime += 1;
                    if (SteamManager.Initialized) {
                        SteamUserStats.GetStat("ATTACK_ENEMY", out int steamEnemyCount);
                        steamEnemyCount++;
                        SteamUserStats.SetStat("ATTACK_ENEMY", steamEnemyCount);
                        SteamUserStats.StoreStats();
                    }
                    attackedCollider.gameObject.GetComponent<EnemyController>().AttackedByPlayer();
                }
            }
        }
    }

    IEnumerator AttackLifeCycle(float bD, float aD) {
        playerAudioManager.SearchSound("PlayerAttacking").audioSource.Play();
        yield return new WaitForSeconds(bD);
        AttackEnemy();
        yield return new WaitForSeconds(aD);
        TurnIsAttackingOff();
    }
    IEnumerator EmergencyLifeCycle() {
        yield return new WaitForSeconds(0.9f);
        playerRigidbody.AddForce(gameObject.transform.right * -0.27f, ForceMode2D.Impulse);
        emergencyCoroutine = null;
    }

    void PlayBgm(double time) {
        currentBgmSoundarray[0].audioSource.PlayScheduled(time);
        if (currentBgmSoundarray[1] != null) {
            currentBgmSoundarray[1].audioSource.PlayScheduled(time + currentBgmSoundarray[0].audioClip.length);
        }
    }

    void StopBgm() {
        if (currentBgmSoundarray[0].audioSource.isPlaying) {
            currentBgmSoundarray[0].audioSource.Stop();
        }
        if (currentBgmSoundarray[1] != null) {
            currentBgmSoundarray[1].audioSource.Stop();
        }
    }

    void SetBgmVolume(float volume) {
        currentBgmSoundarray[0].audioSource.volume = volume;
        if (currentBgmSoundarray[1] != null) {
            currentBgmSoundarray[1].audioSource.volume = volume;
        }
    }

    void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.layer.Equals(7)) {
            touchedSlime = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision) {
        if (collision.gameObject.layer.Equals(7)) {
            touchedSlime = false;
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackingPosition.position, boxSize);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(gameObject.transform.position + new Vector3(-0.5f, 0.07f, 0), gameObject.transform.position + new Vector3(0.5f, 0.07f, 0));
        Gizmos.DrawLine(gameObject.transform.position + new Vector3(-0.5f, -0.07f, 0), gameObject.transform.position + new Vector3(0.5f, -0.07f, 0));
    }

    public void SavePlayer() {
        playTime += Time.timeSinceLevelLoad;
        SaveSystem.SavePlayer(this);
    }

    void OnApplicationQuit() {
        SavePlayer();
    }
}