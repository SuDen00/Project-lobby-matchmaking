using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class NetworkGamePlayer : NetworkBehaviour
{
    [SyncVar] public string playerName;
    [SyncVar] public Role role;
    [SyncVar] public CharacterName passiveAbility;
    [SyncVar] public int health;
    [SyncVar] public int maxHealth;
    [SyncVar] public bool isAlive = true;

    public List<CardData> hand = new List<CardData>();
    public List<CardData> inPlay = new List<CardData>();

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
    }
    /*[Command]
    public void CmdPlayCard(int handIndex, uint targetNetId)
    {
        if (!isAlive) return;
        if (handIndex < 0 || handIndex >= hand.Count) return;
        CardData card = hand[handIndex];
        hand.RemoveAt(handIndex);
        NetworkGamePlayer target = PlayerManager.GetPlayerByNetId(targetNetId);
        TurnManager.Instance.ResolveCardPlay(this, card, target);
    }

    [Command]
    public void CmdEndTurn()
    {
        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPlayer == this)
        {
            TurnManager.Instance.EndTurn();
        }
    }*/

}
