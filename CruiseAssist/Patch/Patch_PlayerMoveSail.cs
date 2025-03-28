using HarmonyLib;
using System;
using UnityEngine;

namespace tanu.CruiseAssist
{
    [HarmonyPatch(typeof(PlayerMove_Sail))]
    public class Patch_PlayerMoveSail
    {

        [HarmonyPatch(nameof(PlayerMove_Sail.GameTick)), HarmonyPrefix]
        public static void GameTick_Prefix(PlayerMove_Sail __instance)
        {
            CruiseAssistor.State = CruiseAssistState.INACTIVE;

            var player = __instance.player;
            if (!player.sailing)
            {
                return;
            }

            if (!CruiseAssistor.Enabled)
            {
                return;
            }

            CruiseAssistor.TargetStar = null;
            CruiseAssistor.TargetPlanet = null;
            CruiseAssistor.TargetEnemy = null;

            if (CruiseAssistor.SelectTargetStar != null)
            {
                // 星系を選択

                if (GameMain.localStar != null && CruiseAssistor.SelectTargetStar.id == GameMain.localStar.id)
                {
                    // 選択した星系の中に居るとき

                    if (CruiseAssistor.SelectTargetPlanet == null && GameMain.localPlanet != null)
                    {
                        // 惑星を未選択で何れかの惑星に居るとき、選択を解除する
                        CruiseAssistor.SelectTargetStar = null;
                        CruiseAssistor.SelectTargetAstroId = 0;
                        GameMain.mainPlayer.navigation.indicatorAstroId = 0;
                        return;
                    }

                    if (CruiseAssistor.SelectTargetPlanet != null)
                    {
                        // 惑星を選択

                        if (GameMain.localPlanet != null && CruiseAssistor.SelectTargetPlanet.id == GameMain.localPlanet.id)
                        {
                            // 選択した惑星に居るとき、選択を解除する
                            CruiseAssistor.SelectTargetStar = null;
                            CruiseAssistor.SelectTargetPlanet = null;
                            CruiseAssistor.SelectTargetAstroId = 0;
                            GameMain.mainPlayer.navigation.indicatorAstroId = 0;
                            return;
                        }

                        // 対象とする
                        CruiseAssistor.TargetPlanet = CruiseAssistor.SelectTargetPlanet;
                    }
                    else if (CruiseAssistor.ReticuleTargetPlanet != null)
                    {
                        // レティクルが惑星を向いているとき、対象とする
                        CruiseAssistor.TargetPlanet = CruiseAssistor.ReticuleTargetPlanet;
                    }
                }
                else
                {
                    // 選択した星系の外に居るとき

                    // 選択した星系を対象とする
                    CruiseAssistor.TargetStar = CruiseAssistor.SelectTargetStar;
                }
            }
            else if (CruiseAssistor.SelectTargetEnemyId != 0)
            {
                CruiseAssistor.TargetEnemy = GameMain.spaceSector.enemyPool[CruiseAssistor.SelectTargetEnemyId];
            }
            else
            {
                // 星系も惑星も未選択

                if (CruiseAssistor.ReticuleTargetPlanet != null)
                {
                    // レティクルが惑星を向いているとき、対象とする
                    CruiseAssistor.TargetPlanet = CruiseAssistor.ReticuleTargetPlanet;
                }
                else if (CruiseAssistor.ReticuleTargetStar != null)
                {
                    // レティクルが星系を向いているとき、対象とする
                    CruiseAssistor.TargetStar = CruiseAssistor.ReticuleTargetStar;
                }
            }

            if (GameMain.mainPlayer.controller.input0 != Vector4.zero || GameMain.mainPlayer.controller.input1 != Vector4.zero)
            {
                return;
            }

            if (CruiseAssistor.TargetPlanet != null)
            {
                CruiseAssistor.State = CruiseAssistState.TO_PLANET;
            }
            else if (CruiseAssistor.TargetStar != null)
            {
                CruiseAssistor.State = CruiseAssistState.TO_STAR;
            }
            else if (CruiseAssistor.TargetEnemy != null)
            {
                CruiseAssistor.State = CruiseAssistState.TO_ENEMY;
                
                var distanceToEnemy = CruiseAssistor.TargetEnemy.Value.pos.Distance(player.uPosition);

                if (GameMain.mainPlayer.warping)
                {
                    GameMain.mainPlayer.controller.actionSail.warpSpeedControl = GetSpeedReducer(distanceToEnemy);

                    if (distanceToEnemy < 3000)
                    {
                        GameMain.mainPlayer.warpCommand = false; // Disable Warp
                    }
                }
            }
            else
            {
                return;
            }

            var astroId = player.navigation.indicatorAstroId; //CruiseAssist.TargetPlanet?.astroId ?? CruiseAssist.TargetStar?.astroId ?? 0;
            var enemyId = player.navigation.indicatorEnemyId; // CruiseAssist.TargetEnemy?.id ?? 0;

            var targetPos = CalculateInterceptVelocity(astroId, enemyId, GameMain.spaceSector, player);

            var angle = Vector3.Angle(targetPos, player.uVelocity);
            var t = 1.6f / Mathf.Max(10f, angle);
            //var speed = player.controller.actionSail.visual_uvel.magnitude;
            player.uVelocity = Vector3.Slerp(player.uVelocity, targetPos.normalized * player.uVelocity.magnitude, t);
        }

        private static readonly double SPEEDCONTROLRANGE = 1 - 0.1;
        private static readonly double SPEEDREDUCTIONRANGEMAX = Math.Log(100000);
        private static readonly double SPEEDREDUCTIONRANGEMIN = Math.Log(1000);
        private static readonly double SPEEDREDUCTIONRANGEMULTIPLIER = SPEEDCONTROLRANGE / (SPEEDREDUCTIONRANGEMAX - SPEEDREDUCTIONRANGEMIN);
        private static readonly double SPEEDREDUCTIONCURVE = (0.1 - (SPEEDREDUCTIONRANGEMULTIPLIER * SPEEDREDUCTIONRANGEMIN));

        private static double GetSpeedReducer(double distance) =>
            Math.Min(1D,
                Math.Max(.1D,
                    SPEEDREDUCTIONCURVE + (SPEEDREDUCTIONRANGEMULTIPLIER * Math.Log(distance))
                        )
                    );

        protected static VectorLF3 CalculateInterceptVelocity(int astroId, int enemyId, SpaceSector sector, Player player)
        {
            // 1. Get the target's current position.
            VectorLF3 targetPos = VectorLF3.zero;
            if (astroId > 1000000)
                targetPos = sector.astros[astroId - 1000000].uPos;
            else if (astroId > 0)
                targetPos = sector.galaxyAstros[astroId].uPos;
            else if (enemyId != 0)
            {
                ref EnemyData local = ref sector.enemyPool[enemyId];
                sector.TransformFromAstro(local.astroId, out targetPos, local.pos);
            }

            // 2. Compute the target's velocity.
            // For astros, assume the target's velocity is the difference between its next position and its current position,
            // scaled by 60 (likely converting per-tick movement to per-second velocity).
            VectorLF3 targetVel = VectorLF3.zero;
            if (astroId > 1000000)
                targetVel = (sector.astros[astroId - 1000000].uPosNext - targetPos) * 60.0;
            else if (astroId > 0)
                targetVel = (sector.galaxyAstros[astroId].uPosNext - targetPos) * 60.0;
            else if (enemyId > 0)
                targetVel = (VectorLF3)sector.enemyPool[enemyId].vel;

            // 3. Get the player's position.
            VectorLF3 playerPos = player.uPosition;

            // 4. Determine the player's effective speed.
            // We combine the player's normal velocity with the warp component.
            VectorLF3 effectivePlayerVel = player.uVelocity + player.controller.actionSail.currentWarpVelocity;
            double s = effectivePlayerVel.magnitude; // This is the desired speed to maintain.

            // 5. Set up the intercept equation.
            // Relative position from player to target:
            VectorLF3 r = targetPos - playerPos;

            // The quadratic coefficients come from:
            //    (|targetVel|^2 - s^2) t^2 + 2 (r dot targetVel) t + |r|^2 = 0
            double A = targetVel.sqrMagnitude - s * s;
            double B = 2.0 * VectorLF3.Dot(r, targetVel);
            double C = r.sqrMagnitude;

            // 6. Solve for time t.
            double t;
            if (Math.Abs(A) < 1e-6)
            {
                // If A is nearly zero, we have a linear equation: B t + C = 0.
                // Avoid division by zero:
                if (Math.Abs(B) < 1e-6)
                    t = 0; // No relative motion—choose zero (or you may handle this case specially)
                else
                    t = -C / B;
            }
            else
            {
                double discriminant = B * B - 4.0 * A * C;
                if (discriminant < 0)
                {
                    // No valid solution (target cannot be intercepted at current speed).
                    // In this case, you might default to aiming directly at the target.
                    t = 0;
                }
                else
                {
                    double sqrtDisc = Math.Sqrt(discriminant);
                    double t1 = (-B + sqrtDisc) / (2.0 * A);
                    double t2 = (-B - sqrtDisc) / (2.0 * A);
                    // Choose the smallest positive time.
                    if (t1 > 0 && t2 > 0)
                        t = Math.Min(t1, t2);
                    else if (t1 > 0)
                        t = t1;
                    else if (t2 > 0)
                        t = t2;
                    else
                        t = Math.Max(t1, t2); // Both negative: intercept in the past; choose one (or handle as needed)
                }
            }

            // 7. Compute the intercept point.
            VectorLF3 interceptPoint = targetPos + targetVel * t;

            // 8. Compute the required velocity: the vector from the player to the intercept point,
            // normalized and scaled to maintain the desired speed.
            VectorLF3 requiredVel = (interceptPoint - playerPos).normalized * s;

            return requiredVel;
        }
    }
}
