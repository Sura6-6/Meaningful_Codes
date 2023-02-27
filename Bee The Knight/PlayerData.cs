[System.Serializable]
public class PlayerData {
    public float positionX;
    public float positionY;
    public float velocityX;
    public float velocityY;
    public bool canControl;
    public float playTime;
    public int fallTime;
    public int enemyAttackTime;
    public int bossAttackTime;

    public PlayerData(PlayerController playerController) {
        positionX = playerController.gameObject.transform.position.x;
        positionY = playerController.gameObject.transform.position.y;
        velocityX = playerController.PlayerRigidbody.velocity.x;
        velocityY = playerController.PlayerRigidbody.velocity.y;
        canControl = playerController.CanControl;
        playTime = playerController.PlayTime;
        fallTime = playerController.FallTime;
        enemyAttackTime = playerController.EnemyAttackTime;
        bossAttackTime = playerController.BossAttackTime;
    }
}