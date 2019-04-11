using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Fly", "Mughisi", "1.2.1")]
    [Description("Allows players with permissions to fly around")]
    public class Fly : HurtworldPlugin
    {
        private readonly Hash<PlayerSession, PlayerInfo> players = new Hash<PlayerSession, PlayerInfo>();

        private const string permAllow = "fly.allowed";

        private CharacterMotorConfiguration defaultConfig;
        private CharacterMotorConfiguration flyingConfig;

        private class PlayerInfo
        {
            public readonly PlayerSession Session;

            public bool IsFlying;
            public float BaseSpeed;

            public PlayerInfo(PlayerSession session)
            {
                Session = session;
                IsFlying = false;
                BaseSpeed = 75f;
            }
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permAllow, this);

            foreach (PlayerSession session in GameManager.Instance.GetSessions().Values)
            {
                players.Add(session, new PlayerInfo(session));
            }
        }

        private void Unload()
        {
            foreach (PlayerInfo player in players.Values)
            {
                if (player.IsFlying)
                {
                    SetFlymode(player.Session);
                }
            }
        }

        private void OnPlayerConnected(PlayerSession session)
        {
            if (!players.ContainsKey(session))
            {
                players.Add(session, new PlayerInfo(session));
            }
        }

        private void OnPlayerDisconnect(PlayerSession session)
        {
            if (players.ContainsKey(session))
            {
                players.Remove(session);
            }
        }

        private void OnPlayerInput(PlayerSession session, InputControls input)
        {
            if (!players.ContainsKey(session) || !players[session].IsFlying)
            {
                return;
            }

            CharacterMotorSimple motor = session.WorldPlayerEntity.GetComponent<CharacterMotorSimple>();
            EntityStats stats = session.WorldPlayerEntity.GetComponent<EntityStats>();

            if (!motor)
            {
                return;
            }

            Vector3 direction = new Vector3(IntFromBool(input.StrafeLeft) * -1 + IntFromBool(input.StrafeRight), 0f, IntFromBool(input.Backward) * -1 + IntFromBool(input.Forward));
            float speed = players[session].BaseSpeed;

            motor.IsGrounded = true;
            direction = motor.RotationToAimDirectionCache * direction.normalized;

            if (input.Forward)
            {
                direction.y = input.DirectionVector.y;
            }

            if (input.Backward)
            {
                direction.y = -input.DirectionVector.y;
            }

            if (input.Sprint)
            {
                speed *= 2;
            }

            if (motor._state.IsCrouching)
            {
                speed /= 2;
            }

            motor.Set_currentVelocity(motor.Accelerate(direction, speed, motor.GetVelocity(), 5, 5));

            if (!stats)
            {
                return;
            }
        }

        [ChatCommand("fly")]
        private void FlyCommand(PlayerSession session, string command, string[] args)
        {
            if (!session.IsAdmin && !permission.UserHasPermission(session.SteamId.ToString(), permAllow))
            {
                SendMessage(session, "No Permission");
                return;
            }

            if (args.Length > 0)
            {
                float speed;

                if (float.TryParse(args[0], out speed))
                {
                    SetFlymode(session, speed);
                    return;
                }
            }

            SetFlymode(session);
        }

        private Vector3 Ground(PlayerSession session)
        {
            CharacterMotorSimple motor = session.WorldPlayerEntity.GetComponent<CharacterMotorSimple>();
            Vector3 loc = motor.PlayerCamera.position;
            Vector3 dir = motor.PlayerCamera.TransformDirection(Vector3.down);
            RaycastHit hit;

            if (Physics.Raycast(loc, dir, out hit))
            {
                return new Vector3(hit.point.x, hit.point.y + 1f, hit.point.z);
            }

            return loc;
        }

        private void SetFlymode(PlayerSession session, float speed = 75f)
        {
            CharacterMotorSimple motor = session.WorldPlayerEntity.GetComponent<CharacterMotorSimple>();

            if (!motor)
            {
                return;
            }

            if (defaultConfig == null && flyingConfig == null)
            {
                SetupMotorConfigs(motor);
            }

            players[session].IsFlying = !players[session].IsFlying;

            if (players[session].IsFlying)
            {
                motor.Config = flyingConfig;
                players[session].BaseSpeed = speed;
            }
            else
            {
                session.WorldPlayerEntity.transform.position = Ground(session);
                motor.Config = defaultConfig;
            }

            AlertManager.Instance.GenericTextNotificationServer(players[session].IsFlying
                    ? lang.GetMessage("Enabled", this, session.SteamId.ToString())
                    : lang.GetMessage("Disabled", this, session.SteamId.ToString()), session.Player);
        }

        private void SetupMotorConfigs(CharacterMotorSimple motor)
        {
            defaultConfig = motor.Config;
            flyingConfig = UnityEngine.Object.Instantiate(defaultConfig);
            flyingConfig.AirSpeedModifier = 0.2f;
            flyingConfig.FallDamageMultiplier = 0f;
            flyingConfig.GravityVector = new Vector3(0f, -25f, 0f);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Enabled", "Fly mode enabled"},
                {"Disabled", "Fly mode disabled"},
                {"No Permission", "You don't have permission to use this command"}
            }, this);
        }

        private int IntFromBool(bool val) => !val ? 0 : 1;

        private void SendMessage(PlayerSession session, string message) => hurt.SendChatMessage(session, null, lang.GetMessage(message, this, session.SteamId.ToString()));
    }
}
