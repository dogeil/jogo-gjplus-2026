using System;
using System.Collections;
using UnityEngine;

/// Ponto do puxador da janela (objeto filho da superfície, com Collider2D pequeno em cima do puxador).
/// Soltar o Alicate aqui com Arame no inventário, janela fechada, roda o minigame de torcer o arame.
public class JanelaArame : Interactable
{
    [SerializeField] Janela janela;

    public override Type RequiredTool => typeof(Alicate);
    protected override bool CanUse => janela != null && janela.Estado == EstadoJanela.Fechada;

    protected override bool LockRoom => false; // olhar pro lado cancela o minigame (WireTwist)

    protected override IEnumerator Minigame(Action<bool> done) => WireTwist.Run(transform.position, room, done);

    protected override void OnSuccess() => janela.ColocarArame();
}
