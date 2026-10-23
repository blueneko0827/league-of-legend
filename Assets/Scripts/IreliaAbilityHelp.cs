using UnityEngine;

namespace MidLane
{
    public static class IreliaAbilityHelp
    {
        // rAimArrow: R is held to aim with an arrow (ScenePresentation "r_aim_indicator"); otherwise
        // it is cast on the key press toward the cursor, like LoL's quick cast.
        public static string Describe(Combatant player, int slot, bool rAimArrow = false)
        {
            BladeDuelist blade = player.Blade; BladeTuning tuning = blade.Tuning;
            string name, body; float range;
            switch (slot)
            {
                case 0:
                    name = "Q · Embate de Cuchillas"; range = tuning.qRange;
                    body = "Daño físico: " + blade.QBaseDamage.ToString("0.#") +
                        "\nContra súbditos: " + (blade.QBaseDamage + blade.QMinionBonusDamage).ToString("0.#") + " (+" + blade.QMinionBonusDamage.ToString("0") + " adicional)." +
                        "\nAntes de armadura; la pasiva y los efectos de impacto de objetos se añaden aparte." +
                        "\n\nSe desliza hacia un enemigo, aplica efectos de impacto y cura a Irelia. Si elimina al objetivo o consume su marca, reinicia Q. Alcance medido de centro a centro. No puede apuntar a torres."; break;
                case 1:
                    name = "W · Danza de Insurrección"; range = tuning.wRange;
                    body = "Mantené W para cargar y reducir el daño recibido. El indicador del suelo sigue el mouse mientras cargás. Soltala para lanzar: causa daño físico en un rectángulo de " + Mathf.RoundToInt(tuning.wHalfWidth * 200) + " unidades de ancho. El daño aumenta con la carga. Mientras carga, Irelia no se mueve ni usa otras habilidades."; break;
                case 2:
                    name = "E · Dueto Perfecto"; range = tuning.eRange;
                    body = "E1 coloca la primera espada; E2 coloca la segunda. La franja mide " + Mathf.RoundToInt(tuning.eHalfWidth * 200) + " unidades de ancho. Convergen y golpean a los enemigos entre ambas, causando daño mágico, aturdimiento de " + tuning.eStun.ToString("0.##") + " s y marca para reiniciar Q. Cada espada se coloca desde la posición actual de Irelia. Tenés " + tuning.eRecastWindow.ToString("0.#") + " s para colocar E2."; break;
                default:
                    name = "R · Filo de Vanguardia"; range = tuning.rRange;
                    body = (rAimArrow ? "Mantené R para apuntar con el indicador del suelo y soltá para lanzar. Clic derecho o Esc cancela. " :
                        "Al presionar R se lanza hacia el cursor, sin flecha de apuntado, como el lanzamiento rápido de LoL. ") + "Lanza una formación de espadas de " + Mathf.RoundToInt(tuning.rHalfWidth * 200) + " unidades de ancho. Al golpear a un campeón explota en un radio de 320 unidades y marca a los campeones durante 5 s. Las espadas caen " + tuning.rWallArmTime.ToString("0.##") + " s después y forman un perímetro con punta, abierto hacia Irelia, durante " + tuning.rWallLifetime.ToString("0.#") + " s; a quien esté sobre la línea al caer lo golpean y lo marcan. Sus paredes tienen " + Mathf.RoundToInt(PracticeAbilityGeometry.IreliaRWallHalfWidth * 200) + " unidades de grosor; cruzarlas causa daño mágico y ralentiza un 90% durante 1,5 s. Se aprende desde nivel 6."; break;
            }
            return name + "\n\n" + body + "\n\nAlcance: " + Mathf.RoundToInt(range * 100) + " unidades LoL · Maná: " + (player.UnlimitedMana ? 0 : player.AbilityCost(slot)) +
                "\nEnfriamiento: " + player.AbilityCooldown(slot).ToString("0.#") + " s · Rango: " + blade.Rank(slot) +
                (blade.Rank(slot) == 0 ? " (sin aprender)" : "") + (player.UnlimitedMana ? "\nModo práctica: reinicio de CD tras 3,5 s sin lanzar habilidades ni causar daño." : player.Progress.PvpSkills ?
                "\nModo PvP: reinicio de CD del jugador tras 3,5 s sin lanzar habilidades ni causar daño. Mejora automática cada 2 bajas totales hasta nivel 18. Prioridad Q, W, E; R en 6/11/16." :
                "\nAprendizaje automático por nivel: Q > W > E; R en 6/11/16.");
        }
    }
}
