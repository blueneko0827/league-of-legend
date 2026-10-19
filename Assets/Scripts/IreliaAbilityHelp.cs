using UnityEngine;

namespace MidLane
{
    public static class IreliaAbilityHelp
    {
        public static string Describe(Combatant player, int slot)
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
                    body = "Mantené R para apuntar con el indicador del suelo y soltá para lanzar. Clic derecho o Esc cancela. Lanza una formación de espadas de " + Mathf.RoundToInt(tuning.rHalfWidth * 200) + " unidades de ancho. Al golpear a un campeón explota en un radio de 320 unidades y marca a los campeones durante 5 s. Forma un perímetro con punta, abierto hacia Irelia, durante " + tuning.rWallLifetime.ToString("0.#") + " s. Sus paredes tienen " + Mathf.RoundToInt(PracticeAbilityGeometry.IreliaRWallHalfWidth * 200) + " unidades de grosor; cruzarlas causa daño mágico y ralentiza un 90% durante 1,5 s. Se aprende desde nivel 6."; break;
            }
            return name + "\n\n" + body + "\n\nAlcance: " + Mathf.RoundToInt(range * 100) + " unidades LoL · Maná: " + (player.UnlimitedMana ? 0 : player.AbilityCost(slot)) +
                "\nEnfriamiento: " + player.AbilityCooldown(slot).ToString("0.#") + " s · Rango: " + blade.Rank(slot) +
                (blade.Rank(slot) == 0 ? " (sin aprender)" : "") + (player.UnlimitedMana ? "\nModo práctica: nivel 6 · reinicio de CD tras 6 s sin daño." : player.Progress.PvpSkills ?
                "\nModo PvP: mejora automática cada 2 bajas totales hasta nivel 18. Prioridad Q, W, E; R en 6/11/16." :
                "\nAprendizaje automático por nivel: Q > W > E; R en 6/11/16.");
        }
    }
}
