using System;
using System.Collections.Generic;
using System.IO;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using System.Globalization;
using System.Threading;
using System.Linq;
using System.Dynamic;
using UnityEngine.Assertions.Must;
using ColossalFramework.PlatformServices;
using ColossalFramework.IO;

namespace CustomizedAI
{

    public class CustomAI : VehicleAI
    {


		public static ushort VID = 8197; // You can change the vehicle ID here to focus on one vehicle
		public static bool AllVehicles = false; // Set to true if you want to get all vehicles
		public static float VangleFront = 0.5f;
		public static float angleMoveFWD = 0.9f;
		public static float angleMoveBWD = -0.9f;
		public static float distanceMagnitude = 40f;
		public static bool onlyVehicles = true;
		public static bool onlyPedestrian = true;

		public static int expandNodes = 3;
		public static int expandPath = 3;
		public static int NUM_BUILD = 10;
		public static int NUM_VEHICLE_PARKED = 10;
		public static int NUM_TREES = 1;
		


        // Implementing our AI from the AI already implemented in game "VehicleAI"

        // COPIED FROM CarAI class, modification are notified
        public override Color GetColor(ushort vehicleID, ref Vehicle data, InfoManager.InfoMode infoMode)
		{
			if (infoMode == InfoManager.InfoMode.NoisePollution)
			{
				int noiseLevel = GetNoiseLevel();
				return CommonBuildingAI.GetNoisePollutionColor((float)noiseLevel * 2.5f);
			}
			return base.GetColor(vehicleID, ref data, infoMode);
		}

		public override void CreateVehicle(ushort vehicleID, ref Vehicle data)
		{
			base.CreateVehicle(vehicleID, ref data);
			if (LeftHandDrive(null))
			{
				data.m_flags |= Vehicle.Flags.LeftHandDrive;
			}
		}

		public override void ReleaseVehicle(ushort vehicleID, ref Vehicle data)
		{
			if ((data.m_flags2 & Vehicle.Flags2.Floating) != 0)
			{
				InstanceID empty = InstanceID.Empty;
				empty.Vehicle = vehicleID;
				InstanceManager.Group group = Singleton<InstanceManager>.instance.GetGroup(empty);
				DisasterHelpers.RemovePeople(group, 0, ref data.m_citizenUnits, 100, ref Singleton<SimulationManager>.instance.m_randomizer);
			}
			base.ReleaseVehicle(vehicleID, ref data);
		}

		public override void SimulationStep(ushort vehicleID, ref Vehicle data, Vector3 physicsLodRefPos)
		{
				

			if ((data.m_flags & Vehicle.Flags.WaitingPath) != 0)
			{
				PathManager instance = Singleton<PathManager>.instance;
				byte pathFindFlags = instance.m_pathUnits.m_buffer[data.m_path].m_pathFindFlags;
				if ((pathFindFlags & 4) != 0)
				{
					data.m_pathPositionIndex = byte.MaxValue;
					data.m_flags &= ~Vehicle.Flags.WaitingPath;
					data.m_flags &= ~Vehicle.Flags.Arriving;
					PathfindSuccess(vehicleID, ref data);
					TrySpawn(vehicleID, ref data);
				}
				else if ((pathFindFlags & 8) != 0)
				{
					data.m_flags &= ~Vehicle.Flags.WaitingPath;
					Singleton<PathManager>.instance.ReleasePath(data.m_path);
					data.m_path = 0u;
					PathfindFailure(vehicleID, ref data);
					return;
				}
			}
			else if ((data.m_flags & Vehicle.Flags.WaitingSpace) != 0)
			{
				TrySpawn(vehicleID, ref data);
			}
			Vector3 lastFramePosition = data.GetLastFramePosition();
			int lodPhysics = (Vector3.SqrMagnitude(physicsLodRefPos - lastFramePosition) >= 1210000f) ? 2 : ((Vector3.SqrMagnitude(Singleton<SimulationManager>.instance.m_simulationView.m_position - lastFramePosition) >= 250000f) ? 1 : 0);
			SimulationStep(vehicleID, ref data, vehicleID, ref data, lodPhysics);
			if (data.m_leadingVehicle == 0 && data.m_trailingVehicle != 0)
			{
				VehicleManager instance2 = Singleton<VehicleManager>.instance;
				ushort num = data.m_trailingVehicle;
				int num2 = 0;
				while (num != 0)
				{
					ushort trailingVehicle = instance2.m_vehicles.m_buffer[num].m_trailingVehicle;
					VehicleInfo info = instance2.m_vehicles.m_buffer[num].Info;
					info.m_vehicleAI.SimulationStep(num, ref instance2.m_vehicles.m_buffer[num], vehicleID, ref data, lodPhysics);
					num = trailingVehicle;
					if (++num2 > 16384)
					{
						CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
						break;
					}
				}
			}
			int privateServiceIndex = ItemClass.GetPrivateServiceIndex(m_info.m_class.m_service);
			int num3 = (privateServiceIndex == -1) ? 150 : 100;
			if ((data.m_flags & (Vehicle.Flags.Spawned | Vehicle.Flags.WaitingPath | Vehicle.Flags.WaitingSpace)) == 0 && data.m_cargoParent == 0)
			{
				Singleton<VehicleManager>.instance.ReleaseVehicle(vehicleID);
			}
			else if (data.m_blockCounter == num3)
			{
				Singleton<VehicleManager>.instance.ReleaseVehicle(vehicleID);
			}
		}

		protected virtual void PathfindSuccess(ushort vehicleID, ref Vehicle data)
		{
		}

		protected virtual void PathfindFailure(ushort vehicleID, ref Vehicle data)
		{
			data.Unspawn(vehicleID);
		}

		public override bool AddWind(ushort vehicleID, ref Vehicle data, Vector3 wind, InstanceManager.Group group)
		{
			if ((data.m_flags & Vehicle.Flags.Spawned) != 0 && data.m_leadingVehicle == 0 && ((data.m_flags2 & Vehicle.Flags2.Blown) != 0 || wind.y > 19.62f))
			{
				Vehicle.Frame lastFrameData = data.GetLastFrameData();
				lastFrameData.m_velocity = lastFrameData.m_velocity * 0.875f + wind * 0.125f;
				wind.y = 0f;
				if (wind.sqrMagnitude > 0.01f)
				{
					lastFrameData.m_rotation = Quaternion.Lerp(lastFrameData.m_rotation, Quaternion.LookRotation(wind), 0.1f);
				}
				data.SetLastFrameData(lastFrameData);
				if ((data.m_flags2 & Vehicle.Flags2.Blown) == 0)
				{
					SetTarget(vehicleID, ref data, 0);
					data.m_flags2 |= Vehicle.Flags2.Blown;
					data.m_waitCounter = 0;
				}
				InstanceID empty = InstanceID.Empty;
				empty.Vehicle = vehicleID;
				Singleton<InstanceManager>.instance.SetGroup(empty, group);
				return true;
			}
			return false;
		}

		public override bool AddWind(ushort parkedID, ref VehicleParked data, Vector3 wind, InstanceManager.Group group)
		{
			if (wind.y > 19.62f)
			{
				VehicleManager instance = Singleton<VehicleManager>.instance;
				CitizenManager instance2 = Singleton<CitizenManager>.instance;
				if (instance.CreateVehicle(out ushort vehicle, ref Singleton<SimulationManager>.instance.m_randomizer, m_info, data.m_position, TransferManager.TransferReason.None, transferToSource: false, transferToTarget: false))
				{
					Vehicle.Frame frameData = instance.m_vehicles.m_buffer[vehicle].m_frame0;
					frameData.m_rotation = data.m_rotation;
					frameData.m_velocity = frameData.m_velocity * 0.875f + wind * 0.125f;
					wind.y = 0f;
					if (wind.sqrMagnitude > 0.01f)
					{
						frameData.m_rotation = Quaternion.Lerp(frameData.m_rotation, Quaternion.LookRotation(wind), 0.1f);
					}
					instance.m_vehicles.m_buffer[vehicle].m_frame0 = frameData;
					instance.m_vehicles.m_buffer[vehicle].m_frame1 = frameData;
					instance.m_vehicles.m_buffer[vehicle].m_frame2 = frameData;
					instance.m_vehicles.m_buffer[vehicle].m_frame3 = frameData;
					m_info.m_vehicleAI.FrameDataUpdated(vehicle, ref instance.m_vehicles.m_buffer[vehicle], ref frameData);
					instance.m_vehicles.m_buffer[vehicle].m_flags2 |= Vehicle.Flags2.Blown;
					uint ownerCitizen = data.m_ownerCitizen;
					instance.m_vehicles.m_buffer[vehicle].m_transferSize = (ushort)(ownerCitizen & 0xFFFF);
					m_info.m_vehicleAI.TrySpawn(vehicle, ref instance.m_vehicles.m_buffer[vehicle]);
					InstanceID empty = InstanceID.Empty;
					empty.ParkedVehicle = parkedID;
					InstanceID empty2 = InstanceID.Empty;
					empty2.Vehicle = vehicle;
					Singleton<InstanceManager>.instance.ChangeInstance(empty, empty2);
					Singleton<InstanceManager>.instance.SetGroup(empty2, group);
					if (ownerCitizen != 0)
					{
						instance2.m_citizens.m_buffer[ownerCitizen].SetParkedVehicle(ownerCitizen, 0);
						instance2.m_citizens.m_buffer[ownerCitizen].SetVehicle(ownerCitizen, vehicle, 0u);
					}
					else
					{
						instance.ReleaseParkedVehicle(parkedID);
					}
					return true;
				}
			}
			return false;
		}

		private void SimulationStepBlown(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
		{
			uint currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;
			frameData.m_position += frameData.m_velocity * 0.5f;
			frameData.m_velocity.y -= 2.4525f;
			frameData.m_velocity *= 0.99f;
			frameData.m_position += frameData.m_velocity * 0.5f;
			float num = Singleton<TerrainManager>.instance.SampleDetailHeight(frameData.m_position);
			if (num > frameData.m_position.y)
			{
				frameData.m_velocity = Vector3.zero;
				frameData.m_position.y = num;
				vehicleData.m_blockCounter = (byte)Mathf.Min(vehicleData.m_blockCounter + 1, 255);
			}
			else
			{
				vehicleData.m_blockCounter = 0;
				frameData.m_travelDistance += 0.1f;
				Randomizer randomizer = new Randomizer(vehicleID);
				float num2 = (float)randomizer.Int32(100, 1000) * 6.283185E-05f * (float)currentFrameIndex;
				Vector3 axis = default(Vector3);
				axis.x = Mathf.Sin((float)randomizer.Int32(1000u) * ((float)Math.PI / 500f) + num2);
				axis.y = Mathf.Sin((float)randomizer.Int32(1000u) * ((float)Math.PI / 500f) + num2);
				axis.z = Mathf.Sin((float)randomizer.Int32(1000u) * ((float)Math.PI / 500f) + num2);
				if (axis.sqrMagnitude > 0.001f)
				{
					Quaternion b = Quaternion.AngleAxis(randomizer.Int32(360u), axis);
					frameData.m_rotation = Quaternion.Lerp(frameData.m_rotation, b, 0.2f);
				}
			}
			bool flag = ((currentFrameIndex + leaderID) & 0x10) != 0;
			leaderData.m_flags &= ~(Vehicle.Flags.OnGravel | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding);
			frameData.m_swayVelocity = Vector3.zero;
			frameData.m_swayPosition = Vector3.zero;
			frameData.m_steerAngle = 0f;
			frameData.m_lightIntensity.x = 5f;
			frameData.m_lightIntensity.y = 5f;
			frameData.m_lightIntensity.z = ((!flag) ? 0f : 5f);
			frameData.m_lightIntensity.w = ((!flag) ? 0f : 5f);
			frameData.m_underground = false;
			frameData.m_transition = false;
		}

		private void SimulationStepFloating(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
		{
			uint currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;
			Vector3 vector = frameData.m_rotation * Vector3.forward;
			frameData.m_position += frameData.m_velocity * 0.5f;
			frameData.m_swayPosition += frameData.m_swayVelocity * 0.5f;
			float num = 0f - Mathf.Atan2(vector.x, vector.z);
			num += frameData.m_angleVelocity * 0.5f;
			frameData.m_rotation = Quaternion.AngleAxis(num * 57.29578f, Vector3.down);
			vehicleData.m_blockCounter = (byte)Mathf.Min(vehicleData.m_blockCounter + 1, 255);
			Singleton<TerrainManager>.instance.SampleWaterData(VectorUtils.XZ(frameData.m_position), out float terrainHeight, out float waterHeight, out Vector3 velocity, out Vector3 normal);
			if (waterHeight - terrainHeight > 1f)
			{
				frameData.m_angleVelocity = frameData.m_angleVelocity * 0.9f + (float)Singleton<SimulationManager>.instance.m_randomizer.Int32(-1000, 1000) * 0.0001f;
				velocity *= 4f / 15f;
				frameData.m_velocity = Vector3.MoveTowards(frameData.m_velocity, velocity, 1f);
				float num2 = Mathf.Clamp((float)(int)vehicleData.m_blockCounter * 0.05f, 1f, waterHeight - terrainHeight);
				frameData.m_velocity.y = frameData.m_velocity.y * 0.5f + (waterHeight - num2 - frameData.m_position.y) * 0.5f;
			}
			else
			{
				terrainHeight = Singleton<TerrainManager>.instance.SampleDetailHeight(frameData.m_position);
				frameData.m_angleVelocity = 0f;
				frameData.m_velocity = Vector3.MoveTowards(frameData.m_velocity, Vector3.zero, 1f);
				frameData.m_velocity.y = frameData.m_velocity.y * 0.5f + (terrainHeight - frameData.m_position.y) * 0.5f;
			}
			normal.y = 0f;
			bool flag = ((currentFrameIndex + leaderID) & 0x10) != 0;
			leaderData.m_flags &= ~(Vehicle.Flags.OnGravel | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding);
			frameData.m_position += frameData.m_velocity * 0.5f;
			num += frameData.m_angleVelocity * 0.5f;
			frameData.m_rotation = Quaternion.AngleAxis(num * 57.29578f, Vector3.down);
			frameData.m_swayVelocity += normal * 0.2f - frameData.m_swayVelocity * 0.5f - frameData.m_swayPosition * 0.5f;
			frameData.m_swayPosition += frameData.m_swayVelocity * 0.5f;
			frameData.m_steerAngle = 0f;
			frameData.m_travelDistance = frameData.m_travelDistance;
			frameData.m_lightIntensity.x = 5f;
			frameData.m_lightIntensity.y = 5f;
			frameData.m_lightIntensity.z = ((!flag) ? 0f : 5f);
			frameData.m_lightIntensity.w = ((!flag) ? 0f : 5f);
			frameData.m_underground = false;
			frameData.m_transition = false;
		}

		public override void SimulationStep(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
		{



			if ((leaderData.m_flags2 & Vehicle.Flags2.Blown) != 0)
			{
				SimulationStepBlown(vehicleID, ref vehicleData, ref frameData, leaderID, ref leaderData, lodPhysics);
				return;
			}
			if ((leaderData.m_flags2 & Vehicle.Flags2.Floating) != 0)
			{
				SimulationStepFloating(vehicleID, ref vehicleData, ref frameData, leaderID, ref leaderData, lodPhysics);
				return;
			}
			uint currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;
			frameData.m_position += frameData.m_velocity * 0.5f;
			frameData.m_swayPosition += frameData.m_swayVelocity * 0.5f;
			float num = m_info.m_acceleration;
			float num2 = m_info.m_braking;
			if ((vehicleData.m_flags & Vehicle.Flags.Emergency2) != 0)
			{
				num *= 2f;
				num2 *= 2f;
			}
			float magnitude = frameData.m_velocity.magnitude;
			Vector3 point = (Vector3)vehicleData.m_targetPos0 - frameData.m_position;
			float sqrMagnitude = point.sqrMagnitude;
			float num3 = (magnitude + num) * (0.5f + 0.5f * (magnitude + num) / num2) + m_info.m_generatedInfo.m_size.z * 0.5f;
			float num4 = Mathf.Max(magnitude + num, 5f);
			if (lodPhysics >= 2 && ((currentFrameIndex >> 4) & 3) == (vehicleID & 3))
			{
				num4 *= 2f;
			}
			float num5 = Mathf.Max((num3 - num4) / 3f, 1f);
			float num6 = num4 * num4;
			float num7 = num5 * num5;
			int index = 0;
			bool flag = false;
			if ((sqrMagnitude < num6 || vehicleData.m_targetPos3.w < 0.01f) && (leaderData.m_flags & (Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped)) == 0)
			{
				if (leaderData.m_path != 0)
				{
					UpdatePathTargetPositions(vehicleID, ref vehicleData, frameData.m_position, ref index, 4, num6, num7);
					if ((leaderData.m_flags & Vehicle.Flags.Spawned) == 0)
					{
						frameData = vehicleData.m_frame0;
						return;
					}
				}
				if ((leaderData.m_flags & Vehicle.Flags.WaitingPath) == 0)
				{
					while (index < 4)
					{
						float minSqrDistance;
						Vector3 refPos;
						if (index == 0)
						{
							minSqrDistance = num6;
							refPos = frameData.m_position;
							flag = true;
						}
						else
						{
							minSqrDistance = num7;
							refPos = vehicleData.GetTargetPos(index - 1);
						}
						int num8 = index;
						UpdateBuildingTargetPositions(vehicleID, ref vehicleData, refPos, leaderID, ref leaderData, ref index, minSqrDistance);
						if (index == num8)
						{
							break;
						}
					}
					if (index != 0)
					{
						Vector4 targetPos = vehicleData.GetTargetPos(index - 1);
						while (index < 4)
						{
							vehicleData.SetTargetPos(index++, targetPos);
						}
					}
				}
				point = (Vector3)vehicleData.m_targetPos0 - frameData.m_position;
				sqrMagnitude = point.sqrMagnitude;
			}
			if (leaderData.m_path != 0 && (leaderData.m_flags & Vehicle.Flags.WaitingPath) == 0)
			{
				NetManager instance = Singleton<NetManager>.instance;
				byte b = leaderData.m_pathPositionIndex;
				byte lastPathOffset = leaderData.m_lastPathOffset;
				if (b == byte.MaxValue)
				{
					b = 0;
				}
				int totalNoise;
				float num9 = 1f + leaderData.CalculateTotalLength(leaderID, out totalNoise);
				PathManager instance2 = Singleton<PathManager>.instance;
				if (instance2.m_pathUnits.m_buffer[leaderData.m_path].GetPosition(b >> 1, out PathUnit.Position position))
				{
					if ((instance.m_segments.m_buffer[position.m_segment].m_flags & NetSegment.Flags.Flooded) != 0 && Singleton<TerrainManager>.instance.HasWater(VectorUtils.XZ(frameData.m_position)))
					{
						leaderData.m_flags2 |= Vehicle.Flags2.Floating;
					}
					instance.m_segments.m_buffer[position.m_segment].AddTraffic(Mathf.RoundToInt(num9 * 2.5f), totalNoise);
					bool flag2 = false;
					if ((b & 1) == 0 || lastPathOffset == 0)
					{
						uint laneID = PathManager.GetLaneID(position);
						if (laneID != 0)
						{
							Vector3 b2 = instance.m_lanes.m_buffer[laneID].CalculatePosition((float)(int)position.m_offset * 0.003921569f);
							float num10 = 0.5f * magnitude * magnitude / num2 + m_info.m_generatedInfo.m_size.z * 0.5f;
							if (Vector3.Distance(frameData.m_position, b2) >= num10 - 1f)
							{
								instance.m_lanes.m_buffer[laneID].ReserveSpace(num9);
								flag2 = true;
							}
						}
					}
					if (!flag2 && instance2.m_pathUnits.m_buffer[leaderData.m_path].GetNextPosition(b >> 1, out position))
					{
						uint laneID2 = PathManager.GetLaneID(position);
						if (laneID2 != 0)
						{
							instance.m_lanes.m_buffer[laneID2].ReserveSpace(num9);
						}
					}
				}
				if (((currentFrameIndex >> 4) & 0xF) == (leaderID & 0xF))
				{
					bool flag3 = false;
					uint unitID = leaderData.m_path;
					int index2 = b >> 1;
					int num11 = 0;
					while (num11 < 5)
					{
						if (PathUnit.GetNextPosition(ref unitID, ref index2, out position, out bool invalid))
						{
							uint laneID3 = PathManager.GetLaneID(position);
							if (laneID3 != 0 && !instance.m_lanes.m_buffer[laneID3].CheckSpace(num9))
							{
								num11++;
								continue;
							}
						}
						if (invalid)
						{
							InvalidPath(vehicleID, ref vehicleData, leaderID, ref leaderData);
						}
						flag3 = true;
						break;
					}
					if (!flag3)
					{
						leaderData.m_flags |= Vehicle.Flags.Congestion;
					}
				}
			}
			float maxSpeed;
			if ((leaderData.m_flags & Vehicle.Flags.Stopped) != 0)
			{
				maxSpeed = 0f;
			}
			else
			{
				maxSpeed = vehicleData.m_targetPos0.w;
				if ((leaderData.m_flags & Vehicle.Flags.DummyTraffic) == 0)
				{
					VehicleManager instance3 = Singleton<VehicleManager>.instance;
					float f = magnitude * 100f / Mathf.Max(1f, vehicleData.m_targetPos0.w);
					instance3.m_totalTrafficFlow += (uint)Mathf.RoundToInt(f);
					instance3.m_maxTrafficFlow += 100u;
				}
			}
			Quaternion rotation = Quaternion.Inverse(frameData.m_rotation);
			point = rotation * point;
			Vector3 vector = rotation * frameData.m_velocity;
			Vector3 a = Vector3.forward;
			Vector3 zero = Vector3.zero;
			Vector3 collisionPush = Vector3.zero;
			float num12 = 0f;
			float num13 = 0f;
			bool blocked = false;
			float len = 0f;
			if (sqrMagnitude > 1f)
			{
				a = VectorUtils.NormalizeXZ(point, out len);
				if (len > 1f)
				{
					Vector3 v = point;
					num4 = Mathf.Max(magnitude, 2f);
					num6 = num4 * num4;
					if (sqrMagnitude > num6)
					{
						v *= num4 / Mathf.Sqrt(sqrMagnitude);
					}
					bool flag4 = false;
					if (v.z < Mathf.Abs(v.x))
					{
						if (v.z < 0f)
						{
							flag4 = true;
						}
						float num14 = Mathf.Abs(v.x);
						if (num14 < 1f)
						{
							v.x = Mathf.Sign(v.x);
							if (v.x == 0f)
							{
								v.x = 1f;
							}
							num14 = 1f;
						}
						v.z = num14;
					}
					a = VectorUtils.NormalizeXZ(v, out float len2);
					len = Mathf.Min(len, len2);
					float num15 = (float)Math.PI / 2f * (1f - a.z);
					if (len > 1f)
					{
						num15 /= len;
					}
					float num16 = len;
					if (vehicleData.m_targetPos0.w < 0.1f)
					{
						maxSpeed = CalculateTargetSpeed(vehicleID, ref vehicleData, 1000f, num15);
						maxSpeed = Mathf.Min(maxSpeed, CalculateMaxSpeed(num16, Mathf.Min(vehicleData.m_targetPos0.w, vehicleData.m_targetPos1.w), num2 * 0.9f));
					}
					else
					{
						maxSpeed = Mathf.Min(maxSpeed, CalculateTargetSpeed(vehicleID, ref vehicleData, 1000f, num15));
						maxSpeed = Mathf.Min(maxSpeed, CalculateMaxSpeed(num16, vehicleData.m_targetPos1.w, num2 * 0.9f));
					}
					num16 += VectorUtils.LengthXZ(vehicleData.m_targetPos1 - vehicleData.m_targetPos0);
					maxSpeed = Mathf.Min(maxSpeed, CalculateMaxSpeed(num16, vehicleData.m_targetPos2.w, num2 * 0.9f));
					num16 += VectorUtils.LengthXZ(vehicleData.m_targetPos2 - vehicleData.m_targetPos1);
					maxSpeed = Mathf.Min(maxSpeed, CalculateMaxSpeed(num16, vehicleData.m_targetPos3.w, num2 * 0.9f));
					num16 += VectorUtils.LengthXZ(vehicleData.m_targetPos3 - vehicleData.m_targetPos2);
					if (vehicleData.m_targetPos3.w < 0.01f)
					{
						num16 = Mathf.Max(0f, num16 - m_info.m_generatedInfo.m_size.z * 0.5f);
					}
					maxSpeed = Mathf.Min(maxSpeed, CalculateMaxSpeed(num16, 0f, num2 * 0.9f));
					if (!DisableCollisionCheck(leaderID, ref leaderData))
					{
						CheckOtherVehicles(vehicleID, ref vehicleData, ref frameData, ref maxSpeed, ref blocked, ref collisionPush, num3, num2 * 0.9f, lodPhysics);
					}
					if (flag4)
					{
						maxSpeed = 0f - maxSpeed;
					}
					if (maxSpeed < magnitude)
					{
						float num17 = Mathf.Max(num, Mathf.Min(num2, magnitude));
						num12 = Mathf.Max(maxSpeed, magnitude - num17);
					}
					else
					{
						float num18 = Mathf.Max(num, Mathf.Min(num2, 0f - magnitude));
						num12 = Mathf.Min(maxSpeed, magnitude + num18);
					}
				}
			}
			else if (magnitude < 0.1f && flag && ArriveAtDestination(leaderID, ref leaderData))
			{
				leaderData.Unspawn(leaderID);
				if (leaderID == vehicleID)
				{
					frameData = leaderData.m_frame0;
				}
				return;
			}
			if ((leaderData.m_flags & Vehicle.Flags.Stopped) == 0 && maxSpeed < 0.1f)
			{
				blocked = true;
			}
			if (blocked)
			{
				vehicleData.m_blockCounter = (byte)Mathf.Min(vehicleData.m_blockCounter + 1, 255);
			}
			else
			{
				vehicleData.m_blockCounter = 0;
			}
			if (len > 1f)
			{
				num13 = Mathf.Asin(a.x) * Mathf.Sign(num12);
				zero = a * num12;
			}
			else
			{
				num12 = 0f;
				Vector3 b3 = Vector3.ClampMagnitude(point * 0.5f - vector, num2);
				zero = vector + b3;
			}
			bool flag5 = ((currentFrameIndex + leaderID) & 0x10) != 0;
			Vector3 a2 = zero - vector;
			Vector3 vector2 = frameData.m_rotation * zero;
			frameData.m_velocity = vector2 + collisionPush;
			frameData.m_position += frameData.m_velocity * 0.5f;
			frameData.m_swayVelocity = frameData.m_swayVelocity * (1f - m_info.m_dampers) - a2 * (1f - m_info.m_springs) - frameData.m_swayPosition * m_info.m_springs;
			frameData.m_swayPosition += frameData.m_swayVelocity * 0.5f;
			frameData.m_steerAngle = num13;
			frameData.m_travelDistance += zero.z;
			frameData.m_lightIntensity.x = 5f;
			frameData.m_lightIntensity.y = ((!(a2.z < -0.1f)) ? 0.5f : 5f);
			frameData.m_lightIntensity.z = ((!(num13 < -0.1f) || !flag5) ? 0f : 5f);
			frameData.m_lightIntensity.w = ((!(num13 > 0.1f) || !flag5) ? 0f : 5f);
			frameData.m_underground = ((vehicleData.m_flags & Vehicle.Flags.Underground) != 0);
			frameData.m_transition = ((vehicleData.m_flags & Vehicle.Flags.Transition) != 0);
			if ((vehicleData.m_flags & Vehicle.Flags.Parking) != 0 && len <= 1f && flag)
			{
				Vector3 forward = vehicleData.m_targetPos1 - vehicleData.m_targetPos0;
				if (forward.sqrMagnitude > 0.01f)
				{
					frameData.m_rotation = Quaternion.LookRotation(forward);
				}
			}
			else if (num12 > 0.1f)
			{
				if (vector2.sqrMagnitude > 0.01f)
				{
					frameData.m_rotation = Quaternion.LookRotation(vector2);
				}
			}
			else if (num12 < -0.1f && vector2.sqrMagnitude > 0.01f)
			{
				frameData.m_rotation = Quaternion.LookRotation(-vector2);
			}
			base.SimulationStep(vehicleID, ref vehicleData, ref frameData, leaderID, ref leaderData, lodPhysics);
		}

		private static bool DisableCollisionCheck(ushort vehicleID, ref Vehicle vehicleData)
		{
			if ((vehicleData.m_flags & Vehicle.Flags.Arriving) != 0)
			{
				float num = Mathf.Max(Mathf.Abs(vehicleData.m_targetPos3.x), Mathf.Abs(vehicleData.m_targetPos3.z));
				float num2 = 8640f;
				if (num > num2 - 100f)
				{
					return true;
				}
			}
			return false;
		}

		protected Vector4 CalculateTargetPoint(Vector3 refPos, Vector3 targetPos, float maxSqrDistance, float speed)
		{
			Vector3 a = targetPos - refPos;
			float sqrMagnitude = a.sqrMagnitude;
			Vector4 result = (!(sqrMagnitude > maxSqrDistance)) ? ((Vector4)targetPos) : ((Vector4)(refPos + a * Mathf.Sqrt(maxSqrDistance / sqrMagnitude)));
			result.w = speed;
			return result;
		}

		public override void FrameDataUpdated(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData)
		{
			Vector3 a = frameData.m_position + frameData.m_velocity * 0.5f;
			Vector3 b = frameData.m_rotation * new Vector3(0f, 0f, Mathf.Max(0.5f, m_info.m_generatedInfo.m_size.z * 0.5f - 1f));
			vehicleData.m_segment.a = a - b;
			vehicleData.m_segment.b = a + b;
		}

		public static void CheckOtherVehicles(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ref float maxSpeed, ref bool blocked, ref Vector3 collisionPush, float maxDistance, float maxBraking, int lodPhysics)
		{
			Vector3 vector = (Vector3)vehicleData.m_targetPos3 - frameData.m_position;
			Vector3 rhs = frameData.m_position + Vector3.ClampMagnitude(vector, maxDistance);
			Vector3 min = Vector3.Min(vehicleData.m_segment.Min(), rhs);
			Vector3 max = Vector3.Max(vehicleData.m_segment.Max(), rhs);
			VehicleManager instance = Singleton<VehicleManager>.instance;
			int num = Mathf.Max((int)((min.x - 10f) / 32f + 270f), 0);
			int num2 = Mathf.Max((int)((min.z - 10f) / 32f + 270f), 0);
			int num3 = Mathf.Min((int)((max.x + 10f) / 32f + 270f), 539);
			int num4 = Mathf.Min((int)((max.z + 10f) / 32f + 270f), 539);
			for (int i = num2; i <= num4; i++)
			{
				for (int j = num; j <= num3; j++)
				{
					ushort num5 = instance.m_vehicleGrid[i * 540 + j];
					int num6 = 0;
					while (num5 != 0)
					{
						num5 = CheckOtherVehicle(vehicleID, ref vehicleData, ref frameData, ref maxSpeed, ref blocked, ref collisionPush, maxBraking, num5, ref instance.m_vehicles.m_buffer[num5], min, max, lodPhysics);
						if (++num6 > 16384)
						{
							CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
				}
			}
			if (lodPhysics != 0)
			{
				return;
			}
			CitizenManager instance2 = Singleton<CitizenManager>.instance;
			float num7 = 0f;
			Vector3 vector2 = vehicleData.m_segment.b;
			Vector3 lhs = vehicleData.m_segment.b - vehicleData.m_segment.a;
			for (int k = 0; k < 4; k++)
			{
				Vector3 vector3 = vehicleData.GetTargetPos(k);
				Vector3 vector4 = vector3 - vector2;
				if (!(Vector3.Dot(lhs, vector4) > 0f))
				{
					continue;
				}
				float magnitude = vector4.magnitude;
				if (magnitude > 0.01f)
				{
					Segment3 segment = new Segment3(vector2, vector3);
					min = segment.Min();
					max = segment.Max();
					int num8 = Mathf.Max((int)((min.x - 3f) / 8f + 1080f), 0);
					int num9 = Mathf.Max((int)((min.z - 3f) / 8f + 1080f), 0);
					int num10 = Mathf.Min((int)((max.x + 3f) / 8f + 1080f), 2159);
					int num11 = Mathf.Min((int)((max.z + 3f) / 8f + 1080f), 2159);
					for (int l = num9; l <= num11; l++)
					{
						for (int m = num8; m <= num10; m++)
						{

							ushort num12 = instance2.m_citizenGrid[l * 2160 + m];
							int num13 = 0;
							while (num12 != 0)
							{
								num12 = CheckCitizen(vehicleID, ref vehicleData, segment, num7, magnitude, ref maxSpeed, ref blocked, maxBraking, num12, ref instance2.m_instances.m_buffer[num12], min, max);
								if (++num13 > 65536)
								{
									CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
									break;
								}
							}
						}
					}
				}
				lhs = vector4;
				num7 += magnitude;
				vector2 = vector3;
			}
		}
		// Output below : This method checks for citizen nearby
		private static ushort CheckCitizen(ushort vehicleID, ref Vehicle vehicleData, Segment3 segment, float lastLen, float nextLen, ref float maxSpeed, ref bool blocked, float maxBraking, ushort otherID, ref CitizenInstance otherData, Vector3 min, Vector3 max)
		{


			if ((vehicleData.m_flags & Vehicle.Flags.Transition) == 0 && (otherData.m_flags & CitizenInstance.Flags.Transition) == 0 && (vehicleData.m_flags & Vehicle.Flags.Underground) != 0 != ((otherData.m_flags & CitizenInstance.Flags.Underground) != 0))
			{
				return otherData.m_nextGridInstance;
			}
			if ((otherData.m_flags & CitizenInstance.Flags.InsideBuilding) != 0)
			{
				return otherData.m_nextGridInstance;
			}
			CitizenInfo info = otherData.Info;
			CitizenInstance.Frame lastFrameData = otherData.GetLastFrameData();
			Vector3 position = lastFrameData.m_position;
			Vector3 b = lastFrameData.m_position + lastFrameData.m_velocity;
			Segment3 segment2 = new Segment3(position, b);
			Vector3 vector = segment2.Min();
			vector.x -= info.m_radius;
			vector.z -= info.m_radius;
			Vector3 vector2 = segment2.Max();
			vector2.x += info.m_radius;
			vector2.y += info.m_height;
			vector2.z += info.m_radius;
			if (min.x < vector2.x + 1f && min.y < vector2.y && min.z < vector2.z + 1f && vector.x < max.x + 1f && vector.y < max.y + 2f && vector.z < max.z + 1f && segment.DistanceSqr(segment2, out float u, out float _) < (1f + info.m_radius) * (1f + info.m_radius))
			{
				/////////// OUTPUT ///////////
				// Checking if the citizen is in front
				bool IsInFront = Vector3.Dot(vehicleData.m_segment.b - vehicleData.m_segment.a, otherData.GetLastFramePosition() - vehicleData.GetLastFramePosition()) > 0.5f;
				// Checking if the citizen car is near 
				bool IsNear = Vector3.Magnitude(vehicleData.GetLastFramePosition() - otherData.GetLastFramePosition()) < 20f;
				if ((vehicleID == VID || AllVehicles) && (IsInFront) && (IsNear) && (onlyPedestrian))
				{

					// Get nearest buildings
					#region Get nearest building
					List<ushort> L = new List<ushort>();
					ushort buildID = CustomGetNearestBuilding(vehicleData.GetLastFramePosition(), L);
					L.Add(buildID);
					for (int i = 0; i <= NUM_BUILD; i++)
					{
						buildID = CustomGetNearestBuilding(vehicleData.GetLastFramePosition(), L);
						L.Add(buildID);
						//Debug.Log(buildID);
					}
					#endregion

					// Get nearest parked vehicles
					#region parked vehicles
					List<ushort> L1 = new List<ushort>();
					ushort ran = GetNearestVehicle(vehicleData.GetLastFramePosition(), L1);
					L1.Add(ran);
					for (int i = 0; i <= NUM_VEHICLE_PARKED; i++)
					{
						ran = GetNearestVehicle(vehicleData.GetLastFramePosition(), L1);
						L1.Add(ran);
						//Debug.Log(ran);
					}
					String strL1 = Concatenate(L1);
					//Debug.Log("parked" + strL1);
					#endregion

					// Get nearest trees (Too much trees to check, freeze the game, NOT WORKING)
					#region nearest trees
					//List<int> L2 = new List<int>();
					//int tree = GetNearestTree(vehicleData.GetLastFramePosition(), L2);
					//L2.Add(tree);
					//for (int i = 0; i <= NUM_TREES; i++)
					//{
					//	tree = GetNearestTree(vehicleData.GetLastFramePosition(), L2);
					//	L2.Add(tree);
					//	//Debug.Log(tree);
					//}
					//String strL2 = ConcatenateINT(L2);
					//Debug.Log("tree" + strL2);
					#endregion

					// Get bezier
					#region
					// Get the road where the vehicle is 
					NetManager netManager = Singleton<NetManager>.instance; // get an instance of Net Manager Class, so we can manipulates nodes
					uint currentPath = vehicleData.m_path; // Getting the current path number of the car
					byte pathposIndex = vehicleData.m_pathPositionIndex; // Getting the current position index, a path is composed of 12 positions (to sum up a position is a small part of the path)
					PathManager pathManager = Singleton<PathManager>.instance; // Get an instance of PathManager class, so we can manipulate paths
					pathManager.m_pathUnits.m_buffer[currentPath].GetPosition(pathposIndex >> 1, out PathUnit.Position positionCAR); // getting the actual position of type 'PathUnit.Position' so that we can manipulate it
					Vector3 center = netManager.m_segments.m_buffer[positionCAR.m_segment].m_bounds.center; // We can already get the center of the road. 'netManager.m_segments.m_buffer[position.m_segment]' enables us to access one road
					ushort startNode = netManager.m_segments.m_buffer[positionCAR.m_segment].m_startNode; // We can get the IDs of the nodes
					ushort endNode = netManager.m_segments.m_buffer[positionCAR.m_segment].m_endNode;
					Bezier3 myBezier = SegmentToBezier(positionCAR.m_segment); // For the implemation, see at the bottom of the file
                    #endregion

                    // Expanding roads
                    #region Expanding roads
                    int countDownNodes = expandNodes;
					List<ushort> segments = new List<ushort>();
					List<ushort> nodes = new List<ushort>();
					nodes.Add(startNode);
					while (countDownNodes >= 1)
					{

						int numNodes = nodes.Count();
						countDownNodes--;
						//Debug.Log("Check1" + numNodes);
						List<ushort> copyNodes = new List<ushort>(nodes);
						for (int i = 0; i < numNodes; i++)
						{
							//Debug.Log("Check2");
							ExpandByOne(copyNodes[i], segments, out nodes);
							//Debug.Log("Check3");
						}

					}

					List<Bezier3> beziersExpanded = new List<Bezier3>();
					foreach (ushort s in segments)
					{
						beziersExpanded.Add(SegmentToBezier(s));
					}
					#endregion

					// Get final destination
					#region Get destination

					while (pathManager.m_pathUnits.m_buffer[currentPath].m_nextPathUnit != 0)
					{
						currentPath = pathManager.m_pathUnits.m_buffer[currentPath].m_nextPathUnit;

					}
					int posCount = pathManager.m_pathUnits.m_buffer[currentPath].m_positionCount;
					ushort destination = pathManager.m_pathUnits.m_buffer[currentPath].GetPosition(posCount - 1).m_segment;

                    #endregion

					// Writing and debugging
                    #region
                    // Open the file 
                    StreamWriter sw = new StreamWriter(@"C:\Users\adeye\Desktop\Output1.txt", true);

					// Debug.Log() is used to print in the game debugger when the game is running 
					Debug.Log(vehicleData.Info.name + ", " +
										vehicleID + "," +
										vehicleData.GetLastFramePosition().x + "," +
										vehicleData.GetLastFramePosition().z + "," +
										vehicleData.GetLastFrameData().m_rotation.eulerAngles.x + "," +
										vehicleData.GetLastFrameData().m_rotation.eulerAngles.y + "," +
										myBezier.a + "," +
										myBezier.b + "," +
										myBezier.c + "," +
										myBezier.d + "," +
								 "Pedestrian" + ", " +
										otherID + "," +
										otherData.GetLastFramePosition().x + "," +
										otherData.GetLastFramePosition().z);

					// Write in the file

					sw.WriteLine("ADEYE" + vehicleID + "," +
										vehicleData.GetLastFramePosition().x + "," +
										vehicleData.GetLastFramePosition().z + "," +
										vehicleData.GetLastFrameData().m_rotation.eulerAngles.x + "," +
										vehicleData.GetLastFrameData().m_rotation.eulerAngles.y + "," +
										myBezier.a + "," +
										myBezier.b + "," +
										myBezier.c + "," +
										myBezier.d + "," +
								 "Pedestrian" + otherID + "," + 
										otherData.GetLastFramePosition());
					
					// Close the file
					sw.Close();
                    #endregion

					// Stats


                }
                /////////// END OUTPUT ///////////

                float num = lastLen + nextLen * u;
				if (num >= 0.01f)
				{
					num -= 2f;
					float b2 = Mathf.Max(1f, CalculateMaxSpeed(num, 0f, maxBraking));
					maxSpeed = Mathf.Min(maxSpeed, b2);

					
				}
			}
			return otherData.m_nextGridInstance;
		}
		// Output below
		private static ushort CheckOtherVehicle(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ref float maxSpeed, ref bool blocked, ref Vector3 collisionPush, float maxBraking, ushort otherID, ref Vehicle otherData, Vector3 min, Vector3 max, int lodPhysics)
		{

			
			if (otherID != vehicleID && vehicleData.m_leadingVehicle != otherID && vehicleData.m_trailingVehicle != otherID)
			{
				


				VehicleInfo info = otherData.Info;
				if (info.m_vehicleType == VehicleInfo.VehicleType.Bicycle)
				{
					return otherData.m_nextGridVehicle;
				}
				if (((vehicleData.m_flags | otherData.m_flags) & Vehicle.Flags.Transition) == 0 && (vehicleData.m_flags & Vehicle.Flags.Underground) != (otherData.m_flags & Vehicle.Flags.Underground))
				{
					return otherData.m_nextGridVehicle;
				}
				Vector3 vector;
				Vector3 vector2;
				if (lodPhysics >= 2)
				{
					vector = otherData.m_segment.Min();
					vector2 = otherData.m_segment.Max();
				}
				else
				{
					vector = Vector3.Min(otherData.m_segment.Min(), otherData.m_targetPos3);
					vector2 = Vector3.Max(otherData.m_segment.Max(), otherData.m_targetPos3);
				}
				if (min.x < vector2.x + 2f && min.y < vector2.y + 2f && min.z < vector2.z + 2f && vector.x < max.x + 2f && vector.y < max.y + 2f && vector.z < max.z + 2f)
				{


					/////////// OUTPUT ///////////
					
					

					
					
					// Opening angle: true if something is in the opening angle
					bool IsInFront = Vector3.Dot(vehicleData.m_segment.b - vehicleData.m_segment.a, otherData.m_segment.a - vehicleData.m_segment.a) > VangleFront;
					// Compare our direction with other vehicles direction : If it is close to our car direction then it is true (so we ignore it)
					bool IsMovingForward = Vector3.Dot((vehicleData.m_segment.b - vehicleData.m_segment.a).normalized, (otherData.m_segment.b - otherData.m_segment.a).normalized) > angleMoveFWD;
					// Same thing than above but for vehicles coming in the other direction
					bool IsMovingBackward = Vector3.Dot((vehicleData.m_segment.b - vehicleData.m_segment.a).normalized, (otherData.m_segment.b - otherData.m_segment.a).normalized) < angleMoveBWD;
					// Compute the distance between our car and an other car : true if it is near 
					bool IsNear = Vector3.Magnitude(vehicleData.GetLastFramePosition() - otherData.GetLastFramePosition()) < distanceMagnitude;
					if ((vehicleID == VID || AllVehicles) && (IsInFront) && (IsNear) && (!IsMovingForward) && !(IsMovingBackward) && (onlyVehicles))
					{

						NetManager netManager = Singleton<NetManager>.instance; // get an instance of Net Manager Class, so we can manipulates nodes, roads..
						
						// Get nearest buildings
						#region Get nearest building
						List<ushort> L = new List<ushort>();
						ushort buildID = CustomGetNearestBuilding(vehicleData.GetLastFramePosition(),L);
						L.Add(buildID);
						for (int i=0; i <= NUM_BUILD; i++)
                        {
							buildID = CustomGetNearestBuilding(vehicleData.GetLastFramePosition(), L);
							L.Add(buildID);
							//Debug.Log(buildID);
                        }
						#endregion

						// Get nearest parked vehicles
						#region
						List<ushort> L1 = new List<ushort>();
						ushort ran = GetNearestVehicle(vehicleData.GetLastFramePosition(),L1);
						L1.Add(ran);
						for (int i = 0; i <= NUM_VEHICLE_PARKED; i++)
						{
							ran = GetNearestVehicle(vehicleData.GetLastFramePosition(), L1);
							L1.Add(ran);
							//Debug.Log(ran);
						}
						String strL1 = Concatenate(L1);
						//Debug.Log("parked" + strL1);
						#endregion

						// Get nearest trees (Too much trees to check, freeze the game, NOT WORKING)
						#region
						//List<int> L2 = new List<int>();
						//int tree = GetNearestTree(vehicleData.GetLastFramePosition(), L2);
						//L2.Add(tree);
						//for (int i = 0; i <= NUM_TREES; i++)
						//{
						//	tree = GetNearestTree(vehicleData.GetLastFramePosition(), L2);
						//	L2.Add(tree);
						//	//Debug.Log(tree);
						//}
						//String strL2 = ConcatenateINT(L2);
						//Debug.Log("tree" + strL2);
						#endregion

						// Getting the Bezier roads
						#region Bezier roads
						// Get the road where the SELF vehicle is 
						
						uint currentPath = vehicleData.m_path; // Getting the current path number of the car
						byte pathposIndex = vehicleData.m_pathPositionIndex; // Getting the current position index, a path is composed of 12 positions (to sum up a position is a small part of the path)
						PathManager pathManager = Singleton<PathManager>.instance; // Get an instance of PathManager class, so we can manipulate paths
						pathManager.m_pathUnits.m_buffer[currentPath].GetPosition(pathposIndex >> 1, out PathUnit.Position position); // getting the actual position of type 'PathUnit.Position' so that we can manipulate it
						Bezier3 myBezier = SegmentToBezier(position.m_segment);

						// Get the road where the OTHER car is
						uint otherCurrentPath = otherData.m_path;
						byte otherPathposIndex = otherData.m_pathPositionIndex;
						pathManager.m_pathUnits.m_buffer[otherCurrentPath].GetPosition(otherPathposIndex >> 1, out PathUnit.Position otherPosition);
						Bezier3 otherBezier = SegmentToBezier(otherPosition.m_segment);
						#endregion
						
						// Expanding roads
						#region Expanding roads
						ushort startNode = netManager.m_segments.m_buffer[position.m_segment].m_startNode;
						int countDownNodes = expandNodes;
						List<ushort> segments = new List<ushort>();
						List<ushort> nodes = new List<ushort>();
						nodes.Add(startNode);
						while (countDownNodes >= 1)
                        {
							
							int numNodes = nodes.Count();
							countDownNodes--;
							//Debug.Log("Check1"+numNodes);
							List<ushort> copyNodes = new List<ushort>(nodes);
							for (int i = 0; i < numNodes; i++)
                            {
								//Debug.Log("Check2");
								ExpandByOne(copyNodes[i], segments,out nodes);
								//Debug.Log("Check3");
							}

                        }
						// Converting them into Bezier roads
						List<Bezier3> beziersExpanded = new List<Bezier3>();
						foreach (ushort s in segments)
                        {
							beziersExpanded.Add(SegmentToBezier(s));
                        }
						#endregion

						// Get final destination
						#region Get destination
						uint currPath = currentPath;
						while (pathManager.m_pathUnits.m_buffer[currPath].m_nextPathUnit !=0)
                        {
							currPath = pathManager.m_pathUnits.m_buffer[currPath].m_nextPathUnit;
						}
						int posCount = pathManager.m_pathUnits.m_buffer[currPath].m_positionCount;
						ushort destination = pathManager.m_pathUnits.m_buffer[currPath].GetPosition(posCount - 1).m_segment;

						#endregion

						// Get the destination only for expanded roads
						#region
						int countDownPath = expandPath;
						int count = pathManager.m_pathUnits.m_buffer[currentPath].m_positionCount;
						int index;
						PathUnit.Position finalPos;
						while (countDownPath >= 1)
						{
							while (GetIndexPosition(position,currentPath,out index) == false && currentPath!=0)
                            {
								currentPath = pathManager.m_pathUnits.m_buffer[currentPath].m_nextPathUnit;

							}
							countDownPath--;


							while (!pathManager.m_pathUnits.m_buffer[currentPath].GetNextPosition(index,out finalPos))
                            {
								currentPath = pathManager.m_pathUnits.m_buffer[currentPath].m_nextPathUnit;
							}
                            
						}
							
							

					
						

						
                        #endregion

                        // Writing and debugging
                        #region Writing and Debugging
                        StreamWriter sw = new StreamWriter(@"C:\Users\adeye\Desktop\Output1.txt", true);
						
						//Debug.Log(vehicleData.Info.name + ", " +
						//				vehicleID + "," +
						//				vehicleData.GetLastFramePosition().x + "," +
						//				vehicleData.GetLastFramePosition().z + "," +
						//				frameData.m_rotation.eulerAngles.x + "," +
						//				frameData.m_rotation.eulerAngles.y + "," +
						//				myBezier.a + "," +
						//				myBezier.b + "," +
						//				myBezier.c + "," +
						//				myBezier.d + "," +
						//				destination+ "," + 
						//				buildID + "," +
						//			//	parkID + "," +
						//			 otherData.Info.name + ", " +
						//				otherID + ", " +
						//				otherData.GetLastFramePosition().x + "," +
						//				otherData.GetLastFramePosition().z + "," +
						//				otherData.GetLastFrameData().m_rotation.eulerAngles.x + "," +
						//				otherData.GetLastFrameData().m_rotation.eulerAngles.y + "," +
						//				myoBezier.a + "," +
						//				myoBezier.b + "," +
						//				myoBezier.c + "," +
						//				myoBezier.d + "," 
										
						//				);
						
						sw.WriteLine(vehicleData.Info.name + ", " +
                                        vehicleID + "," +
                                        vehicleData.GetLastFramePosition().x + "," +
                                        vehicleData.GetLastFramePosition().z + "," +
                                        frameData.m_rotation.x + "," +
                                        frameData.m_rotation.y + "," +
                                        myBezier.a + "," +
                                        myBezier.b + "," +
                                        myBezier.c + "," +
                                        myBezier.d + "," +
                                        destination + "," +
                                        buildID + "," +
                                     //	parkID + "," +
                                     otherData.Info.name + ", " +
                                        otherID + ", " +
                                        otherData.GetLastFramePosition().x + "," +
                                        otherData.GetLastFramePosition().z + "," +
                                        otherData.GetLastFrameData().m_rotation.x + "," +
                                        otherData.GetLastFrameData().m_rotation.y + "," +
                                        otherBezier.a + "," +
										otherBezier.b + "," +
										otherBezier.c + "," +
										otherBezier.d + ","
                                        );
						

						sw.Close();
                        #endregion
                    }
					

						/////////// END OUTPUT ///////////


					Vehicle.Frame lastFrameData = otherData.GetLastFrameData();
					if (lodPhysics < 2)
					{
						float u;
						float v;
						float num = vehicleData.m_segment.DistanceSqr(otherData.m_segment, out u, out v);
						if (num < 4f)
						{
							Vector3 a = vehicleData.m_segment.Position(0.5f);
							Vector3 b = otherData.m_segment.Position(0.5f);
							Vector3 lhs = vehicleData.m_segment.b - vehicleData.m_segment.a;
							if (Vector3.Dot(lhs, a - b) < 0f)
							{
								collisionPush -= lhs.normalized * (0.1f - num * 0.025f);
							}
							else
							{
								collisionPush += lhs.normalized * (0.1f - num * 0.025f);
							}
							blocked = true;
						}
					}
					float num2 = frameData.m_velocity.magnitude + 0.01f;
					float magnitude = lastFrameData.m_velocity.magnitude;
					float num3 = magnitude * (0.5f + 0.5f * magnitude / info.m_braking) + Mathf.Min(1f, magnitude);
					magnitude += 0.01f;
					float num4 = 0f;
					Vector3 vector3 = vehicleData.m_segment.b;
					Vector3 lhs2 = vehicleData.m_segment.b - vehicleData.m_segment.a;
					int num5 = (vehicleData.Info.m_vehicleType == VehicleInfo.VehicleType.Tram) ? 1 : 0;
					for (int i = num5; i < 4; i++)
					{
						Vector3 vector4 = vehicleData.GetTargetPos(i);
						Vector3 vector5 = vector4 - vector3;
						if (!(Vector3.Dot(lhs2, vector5) > 0f))
						{
							continue;
						}
						float magnitude2 = vector5.magnitude;
						Segment3 segment = new Segment3(vector3, vector4);
						min = segment.Min();
						max = segment.Max();
						segment.a.y *= 0.5f;
						segment.b.y *= 0.5f;
						if (magnitude2 > 0.01f && min.x < vector2.x + 2f && min.y < vector2.y + 2f && min.z < vector2.z + 2f && vector.x < max.x + 2f && vector.y < max.y + 2f && vector.z < max.z + 2f)
						{

							Vector3 a2 = otherData.m_segment.a;
							a2.y *= 0.5f;
							if (segment.DistanceSqr(a2, out float u2) < 4f)
							{
								float num6 = Vector3.Dot(lastFrameData.m_velocity, vector5) / magnitude2;
								float num7 = num4 + magnitude2 * u2;
								if (num7 >= 0.01f)
								{

								


									num7 -= num6 + 3f;
									float num8 = Mathf.Max(0f, CalculateMaxSpeed(num7, num6, maxBraking));
									if (num8 < 0.01f)
									{
										blocked = true;

									}
									Vector3 rhs = Vector3.Normalize((Vector3)otherData.m_targetPos0 - otherData.m_segment.a);
									float num9 = 1.2f - 1f / ((float)(int)vehicleData.m_blockCounter * 0.02f + 0.5f);
									if (Vector3.Dot(vector5, rhs) > num9 * magnitude2)
									{
										maxSpeed = Mathf.Min(maxSpeed, num8);
									}
								}
								break;
							}
							if (lodPhysics < 2)
							{
								float num10 = 0f;
								float num11 = num3;
								Vector3 vector6 = otherData.m_segment.b;
								Vector3 lhs3 = otherData.m_segment.b - otherData.m_segment.a;
								int num12 = (info.m_vehicleType == VehicleInfo.VehicleType.Tram) ? 1 : 0;
								bool flag = false;
								for (int j = num12; j < 4; j++)
								{
									if (!(num11 > 0.1f))
									{
										break;
									}
									Vector3 a3;
									if (otherData.m_leadingVehicle != 0)
									{
										if (j != num12)
										{
											break;
										}
										a3 = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[otherData.m_leadingVehicle].m_segment.b;
									}
									else
									{
										a3 = otherData.GetTargetPos(j);
									}
									Vector3 vector7 = Vector3.ClampMagnitude(a3 - vector6, num11);
									if (!(Vector3.Dot(lhs3, vector7) > 0f))
									{
										continue;
									}
									a3 = vector6 + vector7;
									float magnitude3 = vector7.magnitude;
									num11 -= magnitude3;
									Segment3 segment2 = new Segment3(vector6, a3);
									segment2.a.y *= 0.5f;
									segment2.b.y *= 0.5f;
									if (magnitude3 > 0.01f)
									{
										float u3;
										float v2;
										float num13 = (otherID >= vehicleID) ? segment.DistanceSqr(segment2, out u3, out v2) : segment2.DistanceSqr(segment, out v2, out u3);
										if (num13 < 4f)
										{
											float num14 = num4 + magnitude2 * u3;
											float num15 = num10 + magnitude3 * v2 + 0.1f;
											if (num14 >= 0.01f && num14 * magnitude > num15 * num2)
											{
												float num16 = Vector3.Dot(lastFrameData.m_velocity, vector5) / magnitude2;
												if (num14 >= 0.01f)
												{
													num14 -= num16 + 1f + otherData.Info.m_generatedInfo.m_size.z;
													float num17 = Mathf.Max(0f, CalculateMaxSpeed(num14, num16, maxBraking));
													if (num17 < 0.01f)
													{
														blocked = true;
													}
													maxSpeed = Mathf.Min(maxSpeed, num17);
												}
											}
											flag = true;
											break;
										}
									}
									lhs3 = vector7;
									num10 += magnitude3;
									vector6 = a3;
								}
								if (flag)
								{
									break;
								}
							}
						}
						lhs2 = vector5;
						num4 += magnitude2;
						vector3 = vector4;
					}
				}
			}
			return otherData.m_nextGridVehicle;
		}

		private static bool CheckOverlap(Segment3 segment, ushort ignoreVehicle, float maxVelocity)
		{
			VehicleManager instance = Singleton<VehicleManager>.instance;
			Vector3 vector = segment.Min();
			Vector3 vector2 = segment.Max();
			int num = Mathf.Max((int)((vector.x - 10f) / 32f + 270f), 0);
			int num2 = Mathf.Max((int)((vector.z - 10f) / 32f + 270f), 0);
			int num3 = Mathf.Min((int)((vector2.x + 10f) / 32f + 270f), 539);
			int num4 = Mathf.Min((int)((vector2.z + 10f) / 32f + 270f), 539);
			bool overlap = false;
			for (int i = num2; i <= num4; i++)
			{
				for (int j = num; j <= num3; j++)
				{
					ushort num5 = instance.m_vehicleGrid[i * 540 + j];
					int num6 = 0;
					while (num5 != 0)
					{
						num5 = CheckOverlap(segment, ignoreVehicle, maxVelocity, num5, ref instance.m_vehicles.m_buffer[num5], ref overlap);
						if (++num6 > 16384)
						{
							CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
				}
			}
			return overlap;
		}

		private static ushort CheckOverlap(Segment3 segment, ushort ignoreVehicle, float maxVelocity, ushort otherID, ref Vehicle otherData, ref bool overlap)
		{
			if ((ignoreVehicle == 0 || (otherID != ignoreVehicle && otherData.m_leadingVehicle != ignoreVehicle && otherData.m_trailingVehicle != ignoreVehicle)) && segment.DistanceSqr(otherData.m_segment, out float _, out float _) < 4f)
			{
				VehicleInfo info = otherData.Info;
				if (info.m_vehicleType == VehicleInfo.VehicleType.Bicycle)
				{
					return otherData.m_nextGridVehicle;
				}
				if (otherData.GetLastFrameData().m_velocity.sqrMagnitude < maxVelocity * maxVelocity)
				{
					overlap = true;
				}
			}
			return otherData.m_nextGridVehicle;
		}

		private static float CalculateMaxSpeed(float targetDistance, float targetSpeed, float maxBraking)
		{
			float num = 0.5f * maxBraking;
			float num2 = num + targetSpeed;
			return Mathf.Sqrt(Mathf.Max(0f, num2 * num2 + 2f * targetDistance * maxBraking)) - num;
		}

		protected override void InvalidPath(ushort vehicleID, ref Vehicle vehicleData, ushort leaderID, ref Vehicle leaderData)
		{
			vehicleData.m_targetPos0 = vehicleData.m_targetPos3;
			vehicleData.m_targetPos1 = vehicleData.m_targetPos3;
			vehicleData.m_targetPos2 = vehicleData.m_targetPos3;
			vehicleData.m_targetPos3.w = 0f;
			base.InvalidPath(vehicleID, ref vehicleData, leaderID, ref leaderData);
		}

		protected override void UpdateNodeTargetPos(ushort vehicleID, ref Vehicle vehicleData, ushort nodeID, ref NetNode nodeData, ref Vector4 targetPos, int index)
		{
			if ((nodeData.m_flags & NetNode.Flags.LevelCrossing) == 0)
			{
				return;
			}
			if (targetPos.w > 4f)
			{
				targetPos.w = 4f;
			}
			if (index > 0)
			{
				return;
			}
			NetManager instance = Singleton<NetManager>.instance;
			for (int i = 0; i < 7; i++)
			{
				ushort segment = nodeData.GetSegment(i);
				if (segment == 0)
				{
					continue;
				}
				NetInfo info = instance.m_segments.m_buffer[segment].Info;
				if (info.m_class.m_service != ItemClass.Service.PublicTransport || !CalculateCrossing(info, segment, ref instance.m_segments.m_buffer[segment], nodeID, out Vector3 position, out Vector3 direction, out float radius))
				{
					continue;
				}
				for (int j = i + 1; j < 8; j++)
				{
					ushort segment2 = nodeData.GetSegment(j);
					if (segment2 == 0)
					{
						continue;
					}
					NetInfo info2 = instance.m_segments.m_buffer[segment2].Info;
					if (info2.m_class.m_service != ItemClass.Service.PublicTransport || !CalculateCrossing(info2, segment2, ref instance.m_segments.m_buffer[segment2], nodeID, out Vector3 position2, out Vector3 direction2, out float radius2))
					{
						continue;
					}
					NetSegment.CalculateMiddlePoints(position, direction, position2, direction2, smoothStart: true, smoothEnd: true, out Vector3 middlePos, out Vector3 middlePos2);
					float u;
					float num = Mathf.Sqrt(Bezier2.XZ(position, middlePos, middlePos2, position2).DistanceSqr(VectorUtils.XZ(targetPos), out u));
					float num2 = radius + (radius2 - radius) * u;
					if (num < num2 + 1f)
					{
						float num3 = position.y + (position2.y - position.y) * u + 0.1f;
						if (num > num2 - 1f)
						{
							num3 -= (num - num2 + 1f) * 0.3f;
						}
						if (num3 > targetPos.y)
						{
							targetPos.y = num3;
						}
					}
				}
			}
		}

		private static bool CalculateCrossing(NetInfo segmentInfo, ushort segmentID, ref NetSegment segmentData, ushort nodeID, out Vector3 position, out Vector3 direction, out float radius)
		{
			NetManager instance = Singleton<NetManager>.instance;
			position = Vector3.zero;
			direction = Vector3.zero;
			uint num = segmentData.m_lanes;
			float num2 = 0f;
			radius = 0f;

			

			for (int i = 0; i < segmentInfo.m_lanes.Length; i++)
			{
				if (num == 0)
				{
					break;
				}
				if ((segmentInfo.m_lanes[i].m_laneType & (NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle)) != 0)
				{
					if (segmentData.m_startNode == nodeID)
					{
						
						position += instance.m_lanes.m_buffer[num].m_bezier.a;
					
						direction += instance.m_lanes.m_buffer[num].m_bezier.a - instance.m_lanes.m_buffer[num].m_bezier.b;
				
					}
					else
					{
						position += instance.m_lanes.m_buffer[num].m_bezier.d;
						direction += instance.m_lanes.m_buffer[num].m_bezier.d - instance.m_lanes.m_buffer[num].m_bezier.c;
					}
					num2 += 1f;
					radius = Mathf.Max(radius, Mathf.Abs(segmentInfo.m_lanes[i].m_position) + segmentInfo.m_lanes[i].m_width * 0.5f);
				}
				num = instance.m_lanes.m_buffer[num].m_nextLane;
			}
			if (num2 != 0f)
			{
				position /= num2;
				direction = VectorUtils.NormalizeXZ(direction);
				return true;
			}
			return false;
		}

		protected override void CalculateSegmentPosition(ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position nextPosition, PathUnit.Position position, uint laneID, byte offset, PathUnit.Position prevPos, uint prevLaneID, byte prevOffset, int index, out Vector3 pos, out Vector3 dir, out float maxSpeed)
		{
			NetManager instance = Singleton<NetManager>.instance;
			instance.m_lanes.m_buffer[laneID].CalculatePositionAndDirection((float)(int)offset * 0.003921569f, out pos, out dir);
			float num = m_info.m_braking;
			if ((vehicleData.m_flags & Vehicle.Flags.Emergency2) != 0)
			{
				num *= 2f;
			}
			Vehicle.Frame lastFrameData = vehicleData.GetLastFrameData();
			Vector3 position2 = lastFrameData.m_position;
			Vector3 b = instance.m_lanes.m_buffer[prevLaneID].CalculatePosition((float)(int)prevOffset * 0.003921569f);
			float sqrMagnitude = lastFrameData.m_velocity.sqrMagnitude;
			float num2 = 0.5f * sqrMagnitude / num + m_info.m_generatedInfo.m_size.z * 0.5f;
			if (Vector3.Distance(position2, b) >= num2 - 1f)
			{
				Segment3 segment = default(Segment3);
				segment.a = pos;
				ushort num3;
				ushort num4;

				

				if (offset < position.m_offset)
				{
					segment.b = pos + dir.normalized * m_info.m_generatedInfo.m_size.z;
					num3 = instance.m_segments.m_buffer[position.m_segment].m_startNode;
					num4 = instance.m_segments.m_buffer[position.m_segment].m_endNode;
				}
				else
				{
					segment.b = pos - dir.normalized * m_info.m_generatedInfo.m_size.z;
					num3 = instance.m_segments.m_buffer[position.m_segment].m_endNode;
					num4 = instance.m_segments.m_buffer[position.m_segment].m_startNode;
				}
				ushort num5 = (prevOffset != 0) ? instance.m_segments.m_buffer[prevPos.m_segment].m_endNode : instance.m_segments.m_buffer[prevPos.m_segment].m_startNode;
				if (num3 == num5)
				{
					NetNode.Flags flags = instance.m_nodes.m_buffer[num3].m_flags;
					NetLane.Flags flags2 = (NetLane.Flags)instance.m_lanes.m_buffer[prevLaneID].m_flags;
					bool flag = (flags & NetNode.Flags.TrafficLights) != 0;
					bool flag2 = (flags & NetNode.Flags.LevelCrossing) != 0;
					bool flag3 = (flags2 & NetLane.Flags.JoinedJunction) != 0;
					if ((flags2 & (NetLane.Flags.YieldStart | NetLane.Flags.YieldEnd)) != 0 && (flags & (NetNode.Flags.Junction | NetNode.Flags.TrafficLights | NetNode.Flags.OneWayIn)) == NetNode.Flags.Junction && sqrMagnitude > 0.01f && (vehicleData.m_flags & Vehicle.Flags.Emergency2) == 0)
					{
						maxSpeed = 0f;
						return;
					}
					if ((flags & (NetNode.Flags.Junction | NetNode.Flags.OneWayOut | NetNode.Flags.OneWayIn)) == NetNode.Flags.Junction && instance.m_nodes.m_buffer[num3].CountSegments() != 2 && (vehicleData.m_flags & Vehicle.Flags.Emergency2) == 0)
					{
						float len = vehicleData.CalculateTotalLength(vehicleID) + 2f;
						if (!instance.m_lanes.m_buffer[laneID].CheckSpace(len))
						{
							bool flag4 = false;
							if (nextPosition.m_segment != 0 && instance.m_lanes.m_buffer[laneID].m_length < 30f)
							{
								NetNode.Flags flags3 = instance.m_nodes.m_buffer[num4].m_flags;
								if ((flags3 & (NetNode.Flags.Junction | NetNode.Flags.OneWayOut | NetNode.Flags.OneWayIn)) != NetNode.Flags.Junction || instance.m_nodes.m_buffer[num4].CountSegments() == 2)
								{
									uint laneID2 = PathManager.GetLaneID(nextPosition);
									if (laneID2 != 0)
									{
										flag4 = instance.m_lanes.m_buffer[laneID2].CheckSpace(len);
									}
								}
							}
							if (!flag4)
							{
								maxSpeed = 0f;
								return;
							}
						}
					}
					if (flag && (!flag3 || flag2))
					{
						uint currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;
						uint num6 = (uint)(num5 << 8) / 32768u;
						uint num7 = (currentFrameIndex - num6) & 0xFF;
						NetInfo info = instance.m_nodes.m_buffer[num3].Info;
						RoadBaseAI.GetTrafficLightState(num5, ref instance.m_segments.m_buffer[prevPos.m_segment], currentFrameIndex - num6, out RoadBaseAI.TrafficLightState vehicleLightState, out RoadBaseAI.TrafficLightState pedestrianLightState, out bool vehicles, out bool pedestrians);
						if (!vehicles && num7 >= 196)
						{
							vehicles = true;
							RoadBaseAI.SetTrafficLightState(num5, ref instance.m_segments.m_buffer[prevPos.m_segment], currentFrameIndex - num6, vehicleLightState, pedestrianLightState, vehicles, pedestrians);
						}
						if ((vehicleData.m_flags & Vehicle.Flags.Emergency2) == 0 || info.m_class.m_service != ItemClass.Service.Road)
						{
							switch (vehicleLightState)
							{
								case RoadBaseAI.TrafficLightState.RedToGreen:
									if (num7 < 60)
									{
										maxSpeed = 0f;
										return;
									}
									break;
								case RoadBaseAI.TrafficLightState.GreenToRed:
									if (num7 >= 30)
									{
										maxSpeed = 0f;
										return;
									}
									break;
								case RoadBaseAI.TrafficLightState.Red:
									maxSpeed = 0f;
									return;
							}
						}
					}
				}
			}
			NetInfo info2 = instance.m_segments.m_buffer[position.m_segment].Info;
			if (info2.m_lanes != null && info2.m_lanes.Length > position.m_lane)
			{
				maxSpeed = CalculateTargetSpeed(vehicleID, ref vehicleData, info2.m_lanes[position.m_lane].m_speedLimit, instance.m_lanes.m_buffer[laneID].m_curve);
			}
			else
			{
				maxSpeed = CalculateTargetSpeed(vehicleID, ref vehicleData, 1f, 0f);
			}
			if (instance.m_treatWetAsSnow)
			{
				DistrictManager instance2 = Singleton<DistrictManager>.instance;
				byte district = instance2.GetDistrict(pos);
				DistrictPolicies.CityPlanning cityPlanningPolicies = instance2.m_districts.m_buffer[district].m_cityPlanningPolicies;
				if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.StuddedTires) != 0)
				{
					maxSpeed *= 1f - (float)(int)instance.m_segments.m_buffer[position.m_segment].m_wetness * 0.0005882353f;
					instance2.m_districts.m_buffer[district].m_cityPlanningPoliciesEffect |= DistrictPolicies.CityPlanning.StuddedTires;
				}
				else
				{
					maxSpeed *= 1f - (float)(int)instance.m_segments.m_buffer[position.m_segment].m_wetness * 0.00117647066f;
				}
			}
			else
			{
				maxSpeed *= 1f - (float)(int)instance.m_segments.m_buffer[position.m_segment].m_wetness * 0.0005882353f;
			}
			maxSpeed *= 1f + (float)(int)instance.m_segments.m_buffer[position.m_segment].m_condition * 0.0005882353f;
		}

		protected override void CalculateSegmentPosition(ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position position, uint laneID, byte offset, out Vector3 pos, out Vector3 dir, out float maxSpeed)
		{
			NetManager instance = Singleton<NetManager>.instance;
			instance.m_lanes.m_buffer[laneID].CalculatePositionAndDirection((float)(int)offset * 0.003921569f, out pos, out dir);
			NetInfo info = instance.m_segments.m_buffer[position.m_segment].Info;
			if (info.m_lanes != null && info.m_lanes.Length > position.m_lane)
			{
				maxSpeed = CalculateTargetSpeed(vehicleID, ref vehicleData, info.m_lanes[position.m_lane].m_speedLimit, instance.m_lanes.m_buffer[laneID].m_curve);
			}
			else
			{
				maxSpeed = CalculateTargetSpeed(vehicleID, ref vehicleData, 1f, 0f);
			}
			if (instance.m_treatWetAsSnow)
			{
				DistrictManager instance2 = Singleton<DistrictManager>.instance;
				byte district = instance2.GetDistrict(pos);
				DistrictPolicies.CityPlanning cityPlanningPolicies = instance2.m_districts.m_buffer[district].m_cityPlanningPolicies;
				if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.StuddedTires) != 0)
				{
					maxSpeed *= 1f - (float)(int)instance.m_segments.m_buffer[position.m_segment].m_wetness * 0.0005882353f;
					instance2.m_districts.m_buffer[district].m_cityPlanningPoliciesEffect |= DistrictPolicies.CityPlanning.StuddedTires;
				}
				else
				{
					maxSpeed *= 1f - (float)(int)instance.m_segments.m_buffer[position.m_segment].m_wetness * 0.00117647066f;
				}
			}
			else
			{
				maxSpeed *= 1f - (float)(int)instance.m_segments.m_buffer[position.m_segment].m_wetness * 0.0005882353f;
			}
			maxSpeed *= 1f + (float)(int)instance.m_segments.m_buffer[position.m_segment].m_condition * 0.0005882353f;
		}

		protected override bool StartPathFind(ushort vehicleID, ref Vehicle vehicleData, Vector3 startPos, Vector3 endPos)
		{
			return StartPathFind(vehicleID, ref vehicleData, startPos, endPos, startBothWays: true, endBothWays: true, undergroundTarget: false);
		}

		protected virtual bool StartPathFind(ushort vehicleID, ref Vehicle vehicleData, Vector3 startPos, Vector3 endPos, bool startBothWays, bool endBothWays, bool undergroundTarget)
		{
			VehicleInfo info = m_info;
			bool allowUnderground = (vehicleData.m_flags & (Vehicle.Flags.Underground | Vehicle.Flags.Transition)) != 0;
			if (PathManager.FindPathPosition(startPos, ItemClass.Service.Road, NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle, info.m_vehicleType, allowUnderground, requireConnect: false, 32f, out PathUnit.Position pathPosA, out PathUnit.Position pathPosB, out float distanceSqrA, out float _) && PathManager.FindPathPosition(endPos, ItemClass.Service.Road, NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle, info.m_vehicleType, undergroundTarget, requireConnect: false, 32f, out PathUnit.Position pathPosA2, out PathUnit.Position pathPosB2, out float distanceSqrA2, out float _))
			{
				if (!startBothWays || distanceSqrA < 10f)
				{
					pathPosB = default(PathUnit.Position);
				}
				if (!endBothWays || distanceSqrA2 < 10f)
				{
					pathPosB2 = default(PathUnit.Position);
				}
				if (Singleton<PathManager>.instance.CreatePath(out uint unit, ref Singleton<SimulationManager>.instance.m_randomizer, Singleton<SimulationManager>.instance.m_currentBuildIndex, pathPosA, pathPosB, pathPosA2, pathPosB2, default(PathUnit.Position), NetInfo.LaneType.Vehicle, info.m_vehicleType, 20000f, IsHeavyVehicle(), IgnoreBlocked(vehicleID, ref vehicleData), stablePath: false, skipQueue: false, randomParking: false, ignoreFlooded: false, CombustionEngine()))
				{
					if (vehicleData.m_path != 0)
					{
						Singleton<PathManager>.instance.ReleasePath(vehicleData.m_path);
					}
					vehicleData.m_path = unit;
					vehicleData.m_flags |= Vehicle.Flags.WaitingPath;
					return true;
				}
			}
			return false;
		}

		public override bool TrySpawn(ushort vehicleID, ref Vehicle vehicleData)
		{
			if ((vehicleData.m_flags & Vehicle.Flags.Spawned) != 0)
			{
				return true;
			}
			if (CheckOverlap(vehicleData.m_segment, 0, 1000f))
			{
				vehicleData.m_flags |= Vehicle.Flags.WaitingSpace;
				return false;
			}
			vehicleData.Spawn(vehicleID);
			vehicleData.m_flags &= ~Vehicle.Flags.WaitingSpace;
			return true;
		}

		public override int GetNoiseLevel()
		{
			return (!IsHeavyVehicle()) ? 5 : 9;
		}

		protected virtual bool IsHeavyVehicle()
		{
			return false;
		}

		protected virtual bool CombustionEngine()
		{
			return false;
		}

		// New method added here

		public static ushort CustomGetNearestBuilding(Vector3 position, List< ushort> L)
		{
			float num = float.MaxValue;
			ushort result = 0;
			BuildingManager instance = Singleton<BuildingManager>.instance;
			for (ushort num2 = 1; num2 < 49152; num2 = (ushort)(num2 + 1))
			{
				if ((instance.m_buildings.m_buffer[num2].m_flags & (Building.Flags.Created | Building.Flags.Deleted)) == Building.Flags.Created)
				{
					if (!L.Contains(num2))
					{
						BuildingInfo info = instance.m_buildings.m_buffer[num2].Info;
						if (info != null && info.m_placementStyle != ItemClass.Placement.Procedural && info.GetService() != 0 && !typeof(DecorationBuildingAI).IsAssignableFrom(info.GetAI().GetType()) && info.m_class.m_layer != ItemClass.Layer.Markers)
						{
							InstanceID id = default(InstanceID);
							id.Building = num2;
							if (InstanceManager.GetPosition(id, out Vector3 position2, out Quaternion _, out Vector3 _))
							{
								float sqrMagnitude = (position2 - position).sqrMagnitude;
								if (sqrMagnitude < num)
								{
									num = sqrMagnitude;
									result = num2;
								}
							}
						}
					}
				}
			}
			return result;
		}

		public static ushort GetNearestVehicle(Vector3 position, List<ushort> L)
		{
			float num = float.MaxValue;
			ushort result = 0;
			for (ushort num2 = 1; num2 < 32768; num2 = (ushort)(num2 + 1))
			{
				VehicleParked vehicle = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[num2] ;
				if (!L.Contains(num2))
				{
					if (vehicle.m_flags == 1)
					{
						InstanceID id = default(InstanceID);
						id.ParkedVehicle = num2;
						
							float sqrMagnitude = (position - vehicle.m_position).sqrMagnitude;
							if (sqrMagnitude < num)
							{
								num = sqrMagnitude;
								result = num2;
							}
						
					}
				}
			}
			return result;
		}

		public static int GetNearestTree(Vector3 position, List<int> L)
		{
			float num = float.MaxValue;
			int result = 0;
			for (int num2 = 1; num2 < 262144; num2 = (ushort)(num2 + 1))
			{
				TreeInstance treeInstance = Singleton<TreeManager>.instance.m_trees.m_buffer[num2];
				if (!L.Contains(num2))
				{
					if (treeInstance.m_flags == 3857)
					{
						InstanceID id = default(InstanceID);
						id.Tree = (uint)num2;

						float sqrMagnitude = (position - treeInstance.Position).sqrMagnitude;
						if (sqrMagnitude < num)
						{
							num = sqrMagnitude;
							result = (int)num2;
						}

					}
				}
			}
			return result;
		}

		public static String Concatenate(List<ushort> L)
        {
			String str = "";
			foreach (ushort num in L)
            {
				str += num.ToString() + ",";
            }
			return str;
        }

		public static String ConcatenateINT(List<int> L)
		{
			String str = "";
			foreach (int num in L)
			{
				str += num.ToString() + ",";
			}
			return str;
		}

		public static void ExpandByOne(ushort nodeID,  List<ushort> L, out List<ushort> nodes)
        {
			nodes = new List<ushort>();
			NetManager instance = Singleton<NetManager>.instance;
			int numSeg = instance.m_nodes.m_buffer[nodeID].CountSegments();
			for (int i=0; i<numSeg;i++)
            {
				ushort segment = instance.m_nodes.m_buffer[nodeID].GetSegment(i);
				Debug.Log("1"+segment);
				if (!L.Contains(segment))
                {
					L.Add(segment);
					Debug.Log("2"+segment);
					nodes.Add(instance.m_segments.m_buffer[segment].GetOtherNode(nodeID));
                }
            }
			
        }

		public static Bezier3 SegmentToBezier(ushort Segment)
        {
			NetManager instance = Singleton<NetManager>.instance;
			ushort Startnode = instance.m_segments.m_buffer[Segment].m_startNode;
			ushort Endnode = instance.m_segments.m_buffer[Segment].m_endNode;
			Vector3 centerStartNode = instance.m_nodes.m_buffer[Startnode].m_position;
			Vector3 centerEndNode = instance.m_nodes.m_buffer[Endnode].m_position;
			Vector3 startDir = instance.m_segments.m_buffer[Segment].m_startDirection;
			Vector3 endDir = instance.m_segments.m_buffer[Segment].m_endDirection;
			Vector3 A = Vector3.zero;
			Vector3 B = Vector3.zero;
			NetSegment.CalculateMiddlePoints(centerStartNode, startDir, centerEndNode, endDir, false, false, out A, out B);
			return new Bezier3(centerStartNode, A, B, centerEndNode);
		}

		public static bool GetIndexPosition(PathUnit.Position position,uint currentPath,  out int index)
        {
			PathManager instance = Singleton<PathManager>.instance;
			int count = instance.m_pathUnits.m_buffer[currentPath].m_positionCount;
			for (int i = 0; i < count; i++)
            {
				if (position.m_segment == instance.m_pathUnits.m_buffer[currentPath].GetPosition(i).m_segment)
                {
					index = i;
					return true;
                }
            }
			index = 13;
			return false;
		}

	}




}
