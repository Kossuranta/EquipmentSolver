export interface ProfileResponse {
  id: number;
  gameName: string;
  igdbGameId: number;
  gameCoverUrl: string | null;
  description: string | null;
  version: string;
  isPublic: boolean;
  voteScore: number;
  usageCount: number;
  isOwner: boolean;
  ownerName: string;
  slotCount: number;
  statTypeCount: number;
  equipmentCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ProfileDetailResponse {
  id: number;
  gameName: string;
  igdbGameId: number;
  gameCoverUrl: string | null;
  description: string | null;
  version: string;
  isPublic: boolean;
  voteScore: number;
  usageCount: number;
  isOwner: boolean;
  ownerName: string;
  createdAt: string;
  updatedAt: string;
  slots: SlotDto[];
  statTypes: StatTypeDto[];
  equipment: EquipmentDto[];
}

export interface SlotDto {
  id: number;
  name: string;
  sortOrder: number;
}

export interface StatTypeDto {
  id: number;
  name: string;
  displayName: string;
}

export interface EquipmentDto {
  id: number;
  name: string;
  compatibleSlotIds: number[];
  stats: EquipmentStatDto[];
}

export interface EquipmentStatDto {
  statTypeId: number;
  value: number;
}

export interface CreateProfileRequest {
  gameName: string;
  igdbGameId: number;
  gameCoverUrl: string | null;
  description: string | null;
}

export interface UpdateProfileRequest {
  gameName: string;
  igdbGameId: number;
  gameCoverUrl: string | null;
  description: string | null;
}

export interface CreateSlotRequest {
  name: string;
}

export interface UpdateSlotRequest {
  name: string;
}

export interface ReorderSlotsRequest {
  slotIds: number[];
}

export interface CreateStatTypeRequest {
  name: string;
  displayName: string;
}

export interface UpdateStatTypeRequest {
  name: string;
  displayName: string;
}

export interface CreateEquipmentRequest {
  name: string;
  compatibleSlotIds: number[];
  stats: EquipmentStatInput[];
}

export interface UpdateEquipmentRequest {
  name: string;
  compatibleSlotIds: number[];
  stats: EquipmentStatInput[];
}

export interface EquipmentStatInput {
  statTypeId: number;
  value: number;
}

export interface PatchNoteResponse {
  id: number;
  version: string;
  date: string;
  content: string;
}

export interface CreatePatchNoteRequest {
  version: string;
  content: string;
}

export interface UserEquipmentStateResponse {
  equipmentId: number;
  isEnabled: boolean;
}

export interface UserSlotStateResponse {
  slotId: number;
  isEnabled: boolean;
}

export interface GameSearchResult {
  igdbId: number;
  name: string;
  coverUrl: string | null;
}
