export interface ProfileResponse {
  id: number;
  name: string;
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
  name: string;
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
  name: string;
  gameName: string;
  igdbGameId: number;
  gameCoverUrl: string | null;
  description: string | null;
}

export interface UpdateProfileRequest {
  name: string;
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
  displayName: string;
}

export interface UpdateStatTypeRequest {
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

// --- Solver ---

export interface SolveRequest {
  constraints: SolveConstraintInput[];
  priorities: SolvePriorityInput[];
  topN: number;
}

export interface SolveConstraintInput {
  statTypeId: number;
  operator: string;
  value: number;
}

export interface SolvePriorityInput {
  statTypeId: number;
  weight: number;
}

export interface SolveResponse {
  results: SolveResultDto[];
  timedOut: boolean;
  elapsedMs: number;
  combinationsEvaluated: number;
}

export interface SolveResultDto {
  rank: number;
  score: number;
  statTotals: StatTotalDto[];
  assignments: SlotAssignmentDto[];
}

export interface StatTotalDto {
  statTypeId: number;
  statDisplayName: string;
  value: number;
}

export interface SlotAssignmentDto {
  slotId: number;
  slotName: string;
  equipmentId: number | null;
  equipmentName: string | null;
  stats: ItemStatDto[];
}

export interface ItemStatDto {
  statTypeId: number;
  statDisplayName: string;
  value: number;
}

// --- Solver Presets ---

export interface PresetResponse {
  id: number;
  name: string;
  constraints: PresetConstraintDto[];
  priorities: PresetPriorityDto[];
}

export interface PresetConstraintDto {
  id: number;
  statTypeId: number;
  operator: string;
  value: number;
}

export interface PresetPriorityDto {
  id: number;
  statTypeId: number;
  weight: number;
}

export interface CreatePresetRequest {
  name: string;
  constraints: SolveConstraintInput[];
  priorities: SolvePriorityInput[];
}

export interface UpdatePresetRequest {
  name: string;
  constraints: SolveConstraintInput[];
  priorities: SolvePriorityInput[];
}

// --- Social / Browse ---

export interface BrowseProfilesResponse {
  items: BrowseProfileItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface BrowseProfileItem {
  id: number;
  name: string;
  gameName: string;
  igdbGameId: number;
  gameCoverUrl: string | null;
  description: string | null;
  version: string;
  voteScore: number;
  usageCount: number;
  ownerName: string;
  slotCount: number;
  statTypeCount: number;
  equipmentCount: number;
  createdAt: string;
  updatedAt: string;
  userVote: number | null;
  isUsing: boolean;
  isOwner: boolean;
}

export interface PublicProfileDetailResponse {
  id: number;
  name: string;
  gameName: string;
  igdbGameId: number;
  gameCoverUrl: string | null;
  description: string | null;
  version: string;
  voteScore: number;
  usageCount: number;
  ownerName: string;
  isOwner: boolean;
  createdAt: string;
  updatedAt: string;
  userVote: number | null;
  isUsing: boolean;
  slots: SlotDto[];
  statTypes: StatTypeDto[];
  equipment: EquipmentDto[];
  solverPresets: SolverPresetDetailDto[];
  patchNotes: PatchNoteDetailDto[];
}

export interface SolverPresetDetailDto {
  id: number;
  name: string;
  constraints: SolverPresetConstraintDetailDto[];
  priorities: SolverPresetPriorityDetailDto[];
}

export interface SolverPresetConstraintDetailDto {
  statTypeId: number;
  operator: string;
  value: number;
}

export interface SolverPresetPriorityDetailDto {
  statTypeId: number;
  weight: number;
}

export interface PatchNoteDetailDto {
  id: number;
  version: string;
  date: string;
  content: string;
}

export interface VoteResponse {
  newScore: number;
  userVote: number;
}

export interface CopyResponse {
  id: number;
  name: string;
}
