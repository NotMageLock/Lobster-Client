using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Linq;

namespace LobsterClient
{
    public class ModInfo
    {
        public const string name = "Lobster Client";
        public const string guid = "magelock.lobsterclient";
        public const string version = "1.0.0";
    }

    [BepInPlugin(ModInfo.guid, ModInfo.name, ModInfo.version)]
    public class Main : BaseUnityPlugin
    {
        private bool showGUI = false;
        private int maxhp = 100;
        private int prevMaxhp = 100;
        private int playerspeed = 13;
        private bool isInvincible = false;
        private static GameObject muckPlayer;
        private PlayerStatus ps = muckPlayer.GetComponent<PlayerStatus>();
        private PlayerManager muckpm = muckPlayer.GetComponent<PlayerManager>();

        void Awake()
        {
            Harmony harmony = new Harmony(ModInfo.guid);
            harmony.PatchAll();
        }

        void Update()
        {
            if (SceneManager.GetActiveScene().name == "GameAfterLobby")
            {
                if (muckPlayer == null)
                {
                    muckPlayer = GameObject.Find("Player");
                    if (muckPlayer != null)
                    {
                        ps = muckPlayer.GetComponent<PlayerStatus>();
                        muckpm = muckPlayer.GetComponent<PlayerManager>();
                    }
                }

                if (Input.GetKeyDown(KeyCode.R))
                {
                    Heal();
                }

                if (Input.GetKeyDown(KeyCode.L))
                {
                    MineNearestResource();
                }

                if (Input.GetKeyDown(KeyCode.K))
                {
                    KillNearestMob();
                }

                if (Input.GetKeyDown(KeyCode.J))
                {
                    OpenNearestChest();
                }

                if (Input.GetKeyDown(KeyCode.H))
                {
                    GameObject totem = GameObject.Find("TotemRespawn(Clone)/Interact");
                    if (totem != null)
                    {
                        ShrineRespawn rspwn = totem.GetComponent<ShrineRespawn>();
                        rspwn.Interact();
                    }
                }

                if (Input.GetKeyDown(KeyCode.RightControl))
                {
                    showGUI = !showGUI;
                }

                if (isInvincible)
                {
                    Heal();
                }

                MaxHealth();

                Speed();
            }
        }

        void OnGUI()
        {
            if (showGUI)
            {
                GUI.Label(new Rect(10, 10, 200, 20), "Lobster Client");

                int newMaxhp = Mathf.Max(1, Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(10, 40, 200, 20), maxhp, 1, 10000)));
                if (newMaxhp != maxhp)
                {
                    maxhp = newMaxhp;
                    Heal();
                }
                GUI.Label(new Rect(10, 60, 200, 20), "Max HP: " + maxhp);

                playerspeed = Mathf.Max(1, Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(10, 80, 200, 20), playerspeed, 1, 100000)));
                GUI.Label(new Rect(10, 100, 200, 20), "Max Sprint Speed: " + playerspeed);

                isInvincible = GUI.Toggle(new Rect(10, 120, 200, 20), isInvincible, "Invincibility");
            }
        }

        void Heal()
        {
            ps.Respawn();
        }

        void MaxHealth()
        {
            ps.maxHp = maxhp;
        }

        void Speed()
        {
            PlayerMovement muckmove = muckPlayer.GetComponent<PlayerMovement>();
            if (muckmove != null)
            {
                SetMaxRunSpeed(muckmove, playerspeed);
            }
        }

        private void SetMaxRunSpeed(PlayerMovement muckmove, float speed)
        {
            FieldInfo maxRunSpeedField = muckmove.GetType().GetField("maxRunSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
            if (maxRunSpeedField != null)
            {
                maxRunSpeedField.SetValue(muckmove, speed);
            }
        }

        void MineNearestResource()
        {
            if (muckPlayer == null)
            {
                Debug.LogError("muckPlayer reference is not set.");
                return;
            }

            HitableResource[] resources = FindObjectsOfType<HitableResource>();
            if (resources.Length == 0)
            {
                Debug.LogError("No HitableResource objects found.");
                return;
            }

            HitableResource nearestResource = null;
            float minDistance = float.MaxValue;
            Vector3 playerPosition = muckPlayer.transform.position;

            foreach (HitableResource resource in resources)
            {
                float distance = Vector3.Distance(playerPosition, resource.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestResource = resource;
                }
            }

            if (nearestResource != null)
            {
                nearestResource.Hit(10000, 10000f, 0, Vector3.zero, 1);
                Debug.Log("Damage applied to the nearest HitableResource.");
            }
            else
            {
                Debug.LogError("No nearest HitableResource found.");
            }
        }

        void KillNearestMob()
        {
            if (muckPlayer == null)
            {
                Debug.LogError("muckPlayer reference is not set.");
                return;
            }

            HitableMob[] mobsfr = FindObjectsOfType<HitableMob>();
            if (mobsfr.Length == 0)
            {
                Debug.LogError("No mobsfr objects found.");
                return;
            }

            HitableMob nearestmobfr = null;
            float minDistance = float.MaxValue;
            Vector3 playerPosition = muckPlayer.transform.position;

            foreach (HitableMob mobfr in mobsfr)
            {
                float distance = Vector3.Distance(playerPosition, mobfr.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestmobfr = mobfr;
                }
            }

            if (nearestmobfr != null)
            {
                nearestmobfr.Hit(10000, 10000f, 0, Vector3.zero, 1);
                Debug.Log("Damage applied to the nearest Hitablemobfr.");
            }
            else
            {
                Debug.LogError("No nearest Hitablemobfr found.");
            }
        }

        void OpenNearestChest()
        {
            if (muckPlayer == null) return;

            LootContainerInteract[] allLootContainers = FindObjectsOfType<LootContainerInteract>();

            if (allLootContainers.Length == 0) return;

            LootContainerInteract closestContainer = allLootContainers
                .OrderBy(container => Vector3.Distance(muckPlayer.transform.position, container.transform.position))
                .FirstOrDefault();

            if (closestContainer != null)
            {
                SetBasePriceToZero(closestContainer);
            }
        }

        private void SetBasePriceToZero(LootContainerInteract lootContainer)
        {
            FieldInfo basePriceField = lootContainer.GetType().GetField("basePrice", BindingFlags.NonPublic | BindingFlags.Instance);
            if (basePriceField != null)
            {
                basePriceField.SetValue(lootContainer, 0);
            }
        }
    }
}