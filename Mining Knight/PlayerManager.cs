using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class PlayerManager : MonoBehaviour {
    [SerializeField] bool isInCity;
    [SerializeField] bool isField = false;
    int curHealthPoint;
    public int healthIndex = 0;
    public int[] healthPoints;
    public int currentHealthPoint;
    [SerializeField] RuntimeAnimatorController[] playerAnimationControllers;

    public int drillIndex = 0;
    [SerializeField] int[] drillDamages;
    [SerializeField] RuntimeAnimatorController[] drillAnimationControllers;

    public int grndIndex = 0;
    [SerializeField] int[] grndDamages;
    [SerializeField] int[] grndMax;
    [SerializeField] RuntimeAnimatorController[] grndAnimationControllers;

    [SerializeField] int repairAmount;
    public int curRepairPoint;
    public int repairIndex = 0;
    [SerializeField] int[] repairPoints;
    [SerializeField] RuntimeAnimatorController[] droneAnimationControllers;

    public int curDashPoints = 0;
    public int neededDashPoints;

    int inputX = 0;
    bool isDrill = true;
    bool isGuaging = false;
    bool isGuageIncreasing = true;
    enum GRNDSTATE { NONE, GUAGE, WAIT }
    GRNDSTATE grndState = GRNDSTATE.NONE;
    int curGuage = 0;

    Coroutine dashCoroutine = null;
    Coroutine repairCoroutine = null;

    [SerializeField] float speedX;
    [SerializeField] float jetForce;
    [SerializeField] float gravity;
    [SerializeField] float maxSpeedY;
    [SerializeField] float minSpeedY;
    [SerializeField] float dashSpeed;
    [SerializeField] float dashTime;
    [SerializeField] LayerMask EnemyLayer;
    [SerializeField] int maxGuage;
    [SerializeField] Rigidbody2D playerRigidbody;
    [SerializeField] Animator playerAnimator;
    [SerializeField] SpriteRenderer flareRenderer;
    [SerializeField] PlayerInput playerInput;
    [SerializeField] Transform toolTransform;
    [SerializeField] Transform droneTransform;
    [SerializeField] float droneSpeed;
    [SerializeField] GameObject droneRepairEffect;
    [SerializeField] GameObject drill;
    [SerializeField] Animator drillAnimator;
    [SerializeField] GameObject grnd;
    [SerializeField] Animator grndAnimator;
    [SerializeField] GameObject bomb;
    [SerializeField] float bombSpeed;
    [SerializeField] GameObject bar;
    [SerializeField] Slider barSlider;
    [SerializeField] Animator droneAnimator;

    [Header("AUDIO")]
    public AudioSource audio_Dash;
    public AudioSource audio_Heal;
    public AudioSource audio_Jetpack;
    public AudioSource audio_Attacked;

    float grndAngle;
    int curBombAmount = 0;
    int curBombIndex = 0;

    void Start() {
        
        droneTransform.position = gameObject.transform.position - gameObject.transform.right * 0.3f + Vector3.up * 0.3f;
        if (droneTransform.position.x >= gameObject.transform.position.x) {
            droneTransform.eulerAngles = new Vector3(0, 180, 0);
        }
        else {
            droneTransform.eulerAngles = Vector3.zero;
        }

        currentHealthPoint = healthPoints[healthIndex];
        curRepairPoint = repairPoints[repairIndex];
    }

    void FixedUpdate() {
        inputX = 0;
        
        if (gameObject.transform.position.y > 44.5f && isField) {
            SceneManager.LoadScene("UpCityMerge");
        }
        else if (gameObject.transform.position.y < 0.5f && isField) {
            SceneManager.LoadScene("BossMerge");
        }
        if (dashCoroutine == null) {
            playerAnimator.SetInteger("InputY", 0);
            if (isDrill) {
                toolTransform.localEulerAngles = Vector3.zero;
            }
            if (playerInput.actions["Left"].IsPressed() && !playerInput.actions["Right"].IsPressed()) {
                inputX = -1;
                gameObject.transform.eulerAngles = new Vector3(0, 180, 0);
                playerAnimator.SetBool("HasX", true);
            }
            else if (playerInput.actions["Right"].IsPressed() && !playerInput.actions["Left"].IsPressed()) {
                inputX = 1;
                gameObject.transform.eulerAngles = Vector3.zero;
                playerAnimator.SetBool("HasX", true);
            }
            else {
                playerAnimator.SetBool("HasX", false);
            }
            if (playerInput.actions["Up"].IsPressed() && !playerInput.actions["Down"].IsPressed()) {
                playerAnimator.SetInteger("InputY", 1);
                if (isDrill) {
                    toolTransform.localEulerAngles = new Vector3(0, 0, 90);
                }
            }
            else if (playerInput.actions["Down"].IsPressed() && !playerInput.actions["Up"].IsPressed()) {
                playerAnimator.SetInteger("InputY", -1);
                if (isDrill) {
                    toolTransform.localEulerAngles = new Vector3(0, 0, 270);
                }
            }
            if (playerInput.actions["Jet"].IsPressed()) {
                playerRigidbody.AddForce(Vector2.up * jetForce);
                flareRenderer.enabled = true;
            }
            else {
                playerRigidbody.AddForce(Vector2.down * gravity);
                flareRenderer.enabled = false;
            }
            if (playerRigidbody.velocity.y > maxSpeedY) {
                playerRigidbody.velocity = new Vector2(inputX * speedX, maxSpeedY);
            }
            else if (playerRigidbody.velocity.y < minSpeedY) {
                playerRigidbody.velocity = new Vector2(inputX * speedX, minSpeedY);
            }
            else {
                playerRigidbody.velocity = new Vector2(inputX * speedX, playerRigidbody.velocity.y);
            }
            

            if (isGuaging) {
                if (isDrill) {
                    FieldManager.fieldManager.DamageBlock(gameObject.transform.position + toolTransform.right * 0.67f, drillDamages[drillIndex]);
                    Collider2D attackedCollider = Physics2D.OverlapCircle(gameObject.transform.position + toolTransform.right * 0.51f, 0.15f, EnemyLayer);
                    if (attackedCollider != null) {
                        attackedCollider.gameObject.GetComponent<CharacterManager>().GotAttacked(drillDamages[drillIndex]);
                    }
                }
                else {
                    if (curGuage == 0) {
                        isGuageIncreasing = true;
                    }
                    else if (curGuage == maxGuage) {
                        isGuageIncreasing = false;
                    }
                    if (isGuageIncreasing) {
                        curGuage++;
                        barSlider.value = curGuage;
                    }
                    else {
                        curGuage--;
                        barSlider.value = curGuage;
                    }
                }
            }
            if (curDashPoints != neededDashPoints) {
                curDashPoints++;
            }
        }
        if (!isDrill) {
            grndAngle = Vector2.SignedAngle(Vector2.right, Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - gameObject.transform.position);
            toolTransform.eulerAngles = new Vector3(0, 0, grndAngle);
            if (grndAngle > -90 && grndAngle <= 90) {
                grnd.transform.localScale = Vector3.one;
            }
            else {
                grnd.transform.localScale = new Vector3(1, -1, 1);
            }
        }
        droneTransform.position = Vector3.Lerp(droneTransform.position, gameObject.transform.position - gameObject.transform.right * 0.3f + Vector3.up * 0.3f, droneSpeed);
        if (droneTransform.position.x >= gameObject.transform.position.x) {
            droneTransform.eulerAngles = new Vector3(0, 180, 0);
        }
        else {
            droneTransform.eulerAngles = Vector3.zero;
        }
    }
    public void GotAttacked(int dmg)
    {
        if (audio_Attacked != null) audio_Attacked.Play();
        currentHealthPoint -= dmg;
        if (currentHealthPoint <= 0)
        {
            currentHealthPoint = 0;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void Fire(InputAction.CallbackContext ctx) {
        if (ctx.started && !isInCity) {
            if (dashCoroutine == null) {
                if (grndState == GRNDSTATE.NONE) {
                    isGuaging = true;
                    if (isDrill) {
                        drillAnimator.SetBool("IsDrilling", true);
                    }
                    else {
                        bar.SetActive(true);
                        //발사게이지바 Enable
                        curGuage = 0;
                        grndState = GRNDSTATE.GUAGE;
                    }
                }
                else {
                    //수류탄작동
                    bomb.GetComponent<BombController>().ActivateBomb(grndDamages[grndIndex]);
                    bomb.SetActive(false);
                    grndState = GRNDSTATE.NONE;
                }
            }
        }
        else if (ctx.canceled && !isInCity) {
            if (dashCoroutine == null) {
                isGuaging = false;
                if (isDrill) {
                    drillAnimator.SetBool("IsDrilling", false);
                }
                else if (grndState == GRNDSTATE.GUAGE) {
                    grndAnimator.SetTrigger("Fire");
                    //발사게이지바 Disable
                    bar.SetActive(false);
                    //수류탄발사
                    bomb.SetActive(true);
                    bomb.GetComponent<BombController>().throwBomb(curBombIndex, gameObject.transform.position + toolTransform.right * 0.67f, toolTransform.right * bombSpeed * curGuage);
                    curBombAmount--;
                    grndState = GRNDSTATE.WAIT;
                }
            }
        }
    }
    public void Change(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            if (dashCoroutine == null) {
                isGuaging = false;
                if (isDrill) {
                    isDrill = false;
                    drillAnimator.SetBool("IsDrilling", false);
                    drill.SetActive(false);
                    grnd.SetActive(true);
                }
                else {
                    isDrill = true;
                    grnd.SetActive(false);
                    drill.SetActive(true);
                    if (grndState == GRNDSTATE.GUAGE) {
                        //발사게이지바 Disable
                        bar.SetActive(false);
                        grndState = GRNDSTATE.NONE;
                    }
                    else if (grndState == GRNDSTATE.WAIT) {
                        //수류탄작동
                        bomb.GetComponent<BombController>().ActivateBomb(grndDamages[grndIndex]);
                        bomb.SetActive(false);
                        grndState = GRNDSTATE.NONE;
                    }
                }
            }
        }
    }
    public void Repair(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            if (dashCoroutine == null && repairCoroutine == null && curRepairPoint != 0) {
                curRepairPoint--;
                //수리 코루틴
                repairCoroutine = StartCoroutine(RepairLF());
            }
        }
    }
    public void Dash(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            if (dashCoroutine == null && curDashPoints >= neededDashPoints) {
                curDashPoints = curDashPoints - neededDashPoints;
                if (isGuaging) {
                    isGuaging = false;
                    if (grndState == GRNDSTATE.GUAGE) {
                        //발사게이지바 Disable
                        bar.SetActive(false);
                        //발사게이지 초기화
                        grndState = GRNDSTATE.NONE;
                    }
                }
                flareRenderer.enabled = false;
                //대쉬 코루틴
                dashCoroutine = StartCoroutine(DashLF());
            }
        }
    }
    public void Inven(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            //인벤 Enable
        }
        else if (ctx.canceled) {
            //인벤 Disable
        }
    }
    IEnumerator DashLF() {
        if (audio_Dash != null) audio_Dash.Play();
        playerAnimator.SetBool("IsDashing", true);
        if (playerAnimator.GetInteger("InputY") == 1) {
            playerRigidbody.velocity = Vector2.up * dashSpeed;
        }
        else if (playerAnimator.GetInteger("InputY") == -1) {
            playerRigidbody.velocity = Vector2.down * dashSpeed;
        }
        else {
            playerRigidbody.velocity = gameObject.transform.right * dashSpeed;
        }
        yield return new WaitForSeconds(dashTime / 4);
        for (int i = 0; i < 3; i++) {
            if (isDrill && !isInCity) {
                FieldManager.fieldManager.DamageBlock(gameObject.transform.position + toolTransform.right * 0.67f, drillDamages[drillIndex] * 25);
                Collider2D attackedCollider = Physics2D.OverlapCircle(gameObject.transform.position + toolTransform.right * 0.51f, 0.15f, EnemyLayer);
                if (attackedCollider != null) {
                    attackedCollider.gameObject.GetComponent<CharacterManager>().GotAttacked(drillDamages[drillIndex] * 25);
                }
            }
            if (playerAnimator.GetInteger("InputY") == 1) {
                playerRigidbody.velocity = Vector2.up * dashSpeed;
            }
            else if (playerAnimator.GetInteger("InputY") == -1) {
                playerRigidbody.velocity = Vector2.down * dashSpeed;
            }
            else {
                playerRigidbody.velocity = gameObject.transform.right * dashSpeed;
            }
            yield return new WaitForSeconds(dashTime / 4);
        }
        playerAnimator.SetBool("IsDashing", false);
        dashCoroutine = null;
    }
    IEnumerator RepairLF()
    {
        if (audio_Heal != null) audio_Heal.Play();
        droneRepairEffect.SetActive(true);
        yield return new WaitForSeconds(1);
        droneRepairEffect.SetActive(false);
        //체력회복
        curHealthPoint = curHealthPoint + repairAmount;
        if (curHealthPoint > healthPoints[healthIndex]) {
            curHealthPoint = healthPoints[healthIndex];
        }
        repairCoroutine = null;
    }
    public void BombIndex0(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            curBombIndex = 0;
        }
    }
    public void BombIndex1(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            curBombIndex = 1;
        }
    }
    public void BombIndex2(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            curBombIndex = 2;
        }
    }
    public void BombIndex3(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            curBombIndex = 3;
        }
    }
    public void BombIndex4(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            curBombIndex = 4;
        }
    }
    public void UpgradeDrill(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            if (drillIndex == 0) {
                drillIndex = 1;
                drillAnimator.runtimeAnimatorController = drillAnimationControllers[drillIndex];
            }
        }
    }
    public void UpgradeGrnd(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            if (grndIndex == 0) {
                grndIndex = 1;
                grndAnimator.runtimeAnimatorController = grndAnimationControllers[grndIndex];
            }
        }
    }
    public void UpgradeArmor(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            if (healthIndex == 0) {
                healthIndex = 1;
                curHealthPoint = healthPoints[healthIndex];
                playerAnimator.runtimeAnimatorController = playerAnimationControllers[healthIndex];
            }
        }
    }
    public void UpgradeDrone(InputAction.CallbackContext ctx) {
        if (ctx.started) {
            if (repairIndex == 0) {
                repairIndex = 1;
                curRepairPoint = repairPoints[repairIndex];
                droneAnimator.runtimeAnimatorController = droneAnimationControllers[repairIndex];
            }
        }
    }
}
