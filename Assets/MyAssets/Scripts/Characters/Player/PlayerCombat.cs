using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public float attackRange = 2.5f;
    public float damage = 25f;
    public Camera cam;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, attackRange))
            {
                var dmg = hit.collider.GetComponent<IDamageable>();
                if (dmg != null)
                    dmg.TakeDamage(damage, gameObject);
            }
        }
    }
}
