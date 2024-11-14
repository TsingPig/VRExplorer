#if INVECTOR_BASIC || INVECTOR_AI_TEMPLATE
using Invector;
using UnityEngine;
using UnityEngine.Events;

public class ReceiveInvectorDamage : MonoBehaviour, vIDamageReceiver
{
    public OnReceiveDamage onReceiveDamage { get; }
    
    public void TakeDamage(vDamage damage)
    {
        var damageAmount = damage.damageValue;

        // Do your damage code here
        Debug.Log("Took " + damageAmount + " damage.");
    }
}
#endif
