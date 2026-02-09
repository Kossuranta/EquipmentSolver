import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ProfileResponse,
  ProfileDetailResponse,
  CreateProfileRequest,
  UpdateProfileRequest,
  SlotDto,
  CreateSlotRequest,
  UpdateSlotRequest,
  ReorderSlotsRequest,
  StatTypeDto,
  CreateStatTypeRequest,
  UpdateStatTypeRequest,
  EquipmentDto,
  CreateEquipmentRequest,
  UpdateEquipmentRequest,
  PatchNoteResponse,
  CreatePatchNoteRequest,
  UserEquipmentStateResponse,
  UserSlotStateResponse,
  SolveRequest,
  SolveResponse,
  PresetResponse,
  CreatePresetRequest,
  UpdatePresetRequest,
  BulkEquipmentImportRequest,
  BulkImportResponse,
  ProfileExportData,
  ProfileImportResponse,
} from '../models/profile.models';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly apiUrl = `${environment.apiUrl}/profiles`;

  constructor(private readonly http: HttpClient) {}

  // --- Profiles ---

  getMyProfiles(): Observable<ProfileResponse[]> {
    return this.http.get<ProfileResponse[]>(this.apiUrl);
  }

  getProfile(id: number): Observable<ProfileDetailResponse> {
    return this.http.get<ProfileDetailResponse>(`${this.apiUrl}/${id}`);
  }

  createProfile(request: CreateProfileRequest): Observable<ProfileResponse> {
    return this.http.post<ProfileResponse>(this.apiUrl, request);
  }

  updateProfile(id: number, request: UpdateProfileRequest): Observable<ProfileResponse> {
    return this.http.put<ProfileResponse>(`${this.apiUrl}/${id}`, request);
  }

  deleteProfile(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  // --- Slots ---

  getSlots(profileId: number): Observable<SlotDto[]> {
    return this.http.get<SlotDto[]>(`${this.apiUrl}/${profileId}/slots`);
  }

  createSlot(profileId: number, request: CreateSlotRequest): Observable<SlotDto> {
    return this.http.post<SlotDto>(`${this.apiUrl}/${profileId}/slots`, request);
  }

  updateSlot(profileId: number, slotId: number, request: UpdateSlotRequest): Observable<SlotDto> {
    return this.http.put<SlotDto>(`${this.apiUrl}/${profileId}/slots/${slotId}`, request);
  }

  deleteSlot(profileId: number, slotId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${profileId}/slots/${slotId}`);
  }

  reorderSlots(profileId: number, request: ReorderSlotsRequest): Observable<SlotDto[]> {
    return this.http.put<SlotDto[]>(`${this.apiUrl}/${profileId}/slots/reorder`, request);
  }

  // --- Stat Types ---

  getStatTypes(profileId: number): Observable<StatTypeDto[]> {
    return this.http.get<StatTypeDto[]>(`${this.apiUrl}/${profileId}/stat-types`);
  }

  createStatType(profileId: number, request: CreateStatTypeRequest): Observable<StatTypeDto> {
    return this.http.post<StatTypeDto>(`${this.apiUrl}/${profileId}/stat-types`, request);
  }

  updateStatType(
    profileId: number,
    statTypeId: number,
    request: UpdateStatTypeRequest,
  ): Observable<StatTypeDto> {
    return this.http.put<StatTypeDto>(
      `${this.apiUrl}/${profileId}/stat-types/${statTypeId}`,
      request,
    );
  }

  deleteStatType(profileId: number, statTypeId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${profileId}/stat-types/${statTypeId}`);
  }

  // --- Equipment ---

  getEquipment(profileId: number): Observable<EquipmentDto[]> {
    return this.http.get<EquipmentDto[]>(`${this.apiUrl}/${profileId}/equipment`);
  }

  createEquipment(
    profileId: number,
    request: CreateEquipmentRequest,
  ): Observable<EquipmentDto> {
    return this.http.post<EquipmentDto>(`${this.apiUrl}/${profileId}/equipment`, request);
  }

  updateEquipment(
    profileId: number,
    equipmentId: number,
    request: UpdateEquipmentRequest,
  ): Observable<EquipmentDto> {
    return this.http.put<EquipmentDto>(
      `${this.apiUrl}/${profileId}/equipment/${equipmentId}`,
      request,
    );
  }

  deleteEquipment(profileId: number, equipmentId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${profileId}/equipment/${equipmentId}`);
  }

  // --- Patch Notes ---

  getPatchNotes(profileId: number): Observable<PatchNoteResponse[]> {
    return this.http.get<PatchNoteResponse[]>(`${this.apiUrl}/${profileId}/patch-notes`);
  }

  createPatchNote(
    profileId: number,
    request: CreatePatchNoteRequest,
  ): Observable<PatchNoteResponse> {
    return this.http.post<PatchNoteResponse>(`${this.apiUrl}/${profileId}/patch-notes`, request);
  }

  deletePatchNote(profileId: number, noteId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${profileId}/patch-notes/${noteId}`);
  }

  // --- User State ---

  getEquipmentStates(profileId: number): Observable<UserEquipmentStateResponse[]> {
    return this.http.get<UserEquipmentStateResponse[]>(
      `${this.apiUrl}/${profileId}/user-state/equipment`,
    );
  }

  setEquipmentState(profileId: number, equipmentId: number, isEnabled: boolean): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${profileId}/user-state/equipment`, {
      equipmentId,
      isEnabled,
    });
  }

  bulkSetEquipmentState(profileId: number, isEnabled: boolean): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${profileId}/user-state/equipment/bulk`, {
      isEnabled,
    });
  }

  getSlotStates(profileId: number): Observable<UserSlotStateResponse[]> {
    return this.http.get<UserSlotStateResponse[]>(
      `${this.apiUrl}/${profileId}/user-state/slots`,
    );
  }

  setSlotState(profileId: number, slotId: number, isEnabled: boolean): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${profileId}/user-state/slots`, {
      slotId,
      isEnabled,
    });
  }

  // --- Solver ---

  solve(profileId: number, request: SolveRequest): Observable<SolveResponse> {
    return this.http.post<SolveResponse>(`${this.apiUrl}/${profileId}/solver/solve`, request);
  }

  // --- Solver Presets ---

  getPresets(profileId: number): Observable<PresetResponse[]> {
    return this.http.get<PresetResponse[]>(`${this.apiUrl}/${profileId}/solver/presets`);
  }

  getPreset(profileId: number, presetId: number): Observable<PresetResponse> {
    return this.http.get<PresetResponse>(`${this.apiUrl}/${profileId}/solver/presets/${presetId}`);
  }

  createPreset(profileId: number, request: CreatePresetRequest): Observable<PresetResponse> {
    return this.http.post<PresetResponse>(`${this.apiUrl}/${profileId}/solver/presets`, request);
  }

  updatePreset(
    profileId: number,
    presetId: number,
    request: UpdatePresetRequest,
  ): Observable<PresetResponse> {
    return this.http.put<PresetResponse>(
      `${this.apiUrl}/${profileId}/solver/presets/${presetId}`,
      request,
    );
  }

  deletePreset(profileId: number, presetId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${profileId}/solver/presets/${presetId}`);
  }

  // --- Import/Export ---

  downloadCsvTemplate(profileId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${profileId}/equipment/csv-template`, {
      responseType: 'blob',
    });
  }

  bulkImportEquipment(
    profileId: number,
    request: BulkEquipmentImportRequest,
  ): Observable<BulkImportResponse> {
    return this.http.post<BulkImportResponse>(
      `${this.apiUrl}/${profileId}/equipment/import`,
      request,
    );
  }

  exportProfile(profileId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${profileId}/export`, {
      responseType: 'blob',
    });
  }

  importProfileAsNew(data: ProfileExportData): Observable<ProfileImportResponse> {
    return this.http.post<ProfileImportResponse>(`${this.apiUrl}/import`, data);
  }

  replaceProfile(profileId: number, data: ProfileExportData): Observable<ProfileImportResponse> {
    return this.http.put<ProfileImportResponse>(`${this.apiUrl}/${profileId}/import`, data);
  }
}
