using UnityEngine;

#if UNITY_EDITOR
[ExecuteAlways]
public class SpawnGizmo : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 10f);
    }
}
#endif

