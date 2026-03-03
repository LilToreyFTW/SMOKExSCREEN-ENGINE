#pragma once

// Util defs
#define printf(fmt, ...) DbgPrint("[dbg] "fmt, ##__VA_ARGS__)
#define LENGTH(a) (sizeof(a) / sizeof(a[0]))

#define IOCTL_NVIDIA_SMIL (0x8DE0008)
#define IOCTL_NVIDIA_SMIL_MAX (512)

typedef struct _IOC_REQUEST {
	PVOID Buffer;
	ULONG BufferLength;
	PVOID OldContext;
	PIO_COMPLETION_ROUTINE OldRoutine;
} IOC_REQUEST, *PIOC_REQUEST;

PCHAR LowerStr(PCHAR str);
DWORD Random(PDWORD seed);
PVOID SafeCopy(PVOID src, DWORD size);
VOID SpoofBuffer(DWORD seed, PBYTE buffer, DWORD length);
PWCHAR TrimGUID(PWCHAR guid, DWORD max);
VOID ChangeIoc(PIO_STACK_LOCATION ioc, PIRP irp, PIO_COMPLETION_ROUTINE routine);
VOID SwapEndianess(PCHAR dest, PCHAR src);
PVOID FindPattern(PCHAR base, DWORD length, PCHAR pattern, PCHAR mask);
PVOID FindPatternImage(PCHAR base, PCHAR pattern, PCHAR mask);
PVOID GetBaseAddress(PCHAR name, PULONG out_size);

// Win defs
#define IsListEmtpy(list) (list == list->Flink)

typedef NTSTATUS(__fastcall *DISK_FAIL_PREDICTION)(PVOID device_extension, BYTE enable);
typedef NTSTATUS(__fastcall *RU_REGISTER_INTERFACES)(PVOID device_extension);
extern POBJECT_TYPE *IoDriverObjectType;
NTKERNELAPI NTSTATUS ObReferenceObjectByName(IN PUNICODE_STRING ObjectName, IN ULONG Attributes, IN PACCESS_STATE PassedAccessState, IN ACCESS_MASK DesiredAccess, IN POBJECT_TYPE ObjectType, IN KPROCESSOR_MODE AccessMode, IN OUT PVOID ParseContext, OUT PVOID * Object);
NTSTATUS NTAPI ZwQuerySystemInformation(ULONG InfoClass, PVOID Buffer, ULONG Length, PULONG ReturnLength);

// dt ndis!_NDIS_IF_BLOCK
typedef struct _NDIS_IF_BLOCK {
	char _padding_0[0x464];
	IF_PHYSICAL_ADDRESS_LH ifPhysAddress; // 0x464
	IF_PHYSICAL_ADDRESS_LH PermanentPhysAddress; // 0x486
} NDIS_IF_BLOCK, *PNDIS_IF_BLOCK;

typedef struct _KSTRING {
	char _padding_0[0x10];
	WCHAR Buffer[1]; // 0x10 at least
} KSTRING, *PKSTRING;

// dt ndis!_NDIS_FILTER_BLOCK
typedef struct _NDIS_FILTER_BLOCK {
	char _padding_0[0x8];
	struct _NDIS_FILTER_BLOCK *NextFilter; // 0x8
	char _padding_1[0x18];
	PKSTRING FilterInstanceName; // 0x28
} NDIS_FILTER_BLOCK, *PNDIS_FILTER_BLOCK;

typedef struct _STOR_SCSI_IDENTITY {
	INQUIRYDATA *InquiryData;
	STRING SerialNumber;
	CHAR Supports1667;
	CHAR ZonedDevice;
} STOR_SCSI_IDENTITY, *PSTOR_SCSI_IDENTITY;

#define IOCTL_NSI_PROXY_ARP (0x0012001B)
#define NSI_PARAMS_ARP (11)
typedef struct _NSI_PARAMS {
	char _padding_0[0x18];
	ULONG Type; // 0x18
} NSI_PARAMS, *PNSI_PARAMS;

typedef struct _IDSECTOR {
	USHORT  wGenConfig;
	USHORT  wNumCyls;
	USHORT  wReserved;
	USHORT  wNumHeads;
	USHORT  wBytesPerTrack;
	USHORT  wBytesPerSector;
	USHORT  wSectorsPerTrack;
	USHORT  wVendorUnique[3];
	CHAR    sSerialNumber[20];
	USHORT  wBufferType;
	USHORT  wBufferSize;
	USHORT  wECCSize;
	CHAR    sFirmwareRev[8];
	CHAR    sModelNumber[40];
	USHORT  wMoreVendorUnique;
	USHORT  wDoubleWordIO;
	USHORT  wCapabilities;
	USHORT  wReserved1;
	USHORT  wPIOTiming;
	USHORT  wDMATiming;
	USHORT  wBS;
	USHORT  wNumCurrentCyls;
	USHORT  wNumCurrentHeads;
	USHORT  wNumCurrentSectorsPerTrack;
	ULONG   ulCurrentSectorCapacity;
	USHORT  wMultSectorStuff;
	ULONG   ulTotalAddressableSectors;
	USHORT  wSingleWordDMA;
	USHORT  wMultiWordDMA;
	BYTE    bReserved[128];
} IDSECTOR, *PIDSECTOR;

typedef struct _KLDR_DATA_TABLE_ENTRY {
	LIST_ENTRY InLoadOrderLinks;
	PVOID ExceptionTable;
	ULONG ExceptionTableSize;
	PVOID GpValue;
	PVOID NonPagedDebugInfo;
	PVOID DllBase;
	PVOID EntryPoint;
	ULONG SizeOfImage;
	UNICODE_STRING FullDllName;
	UNICODE_STRING BaseDllName;
	ULONG Flags;
	USHORT LoadCount;
	USHORT __Unused;
	PVOID SectionPointer;
	ULONG CheckSum;
	PVOID LoadedImports;
	PVOID PatchInformation;
} KLDR_DATA_TABLE_ENTRY, *PKLDR_DATA_TABLE_ENTRY;

typedef struct _SYSTEM_MODULE {
	HANDLE Section;
	PVOID MappedBase;
	PVOID ImageBase;
	ULONG ImageSize;
	ULONG Flags;
	USHORT LoadOrderIndex;
	USHORT InitOrderIndex;
	USHORT LoadCount;
	USHORT OffsetToFileName;
	UCHAR  FullPathName[MAXIMUM_FILENAME_LENGTH];
} SYSTEM_MODULE, *PSYSTEM_MODULE;

typedef struct _SYSTEM_MODULE_INFORMATION  {
	ULONG NumberOfModules;
	SYSTEM_MODULE Modules[1];
} SYSTEM_MODULE_INFORMATION, *PSYSTEM_MODULE_INFORMATION;

typedef enum _SYSTEM_INFORMATION_CLASS {
	SystemBasicInformation = 0x0,
	SystemProcessorInformation = 0x1,
	SystemPerformanceInformation = 0x2,
	SystemTimeOfDayInformation = 0x3,
	SystemPathInformation = 0x4,
	SystemProcessInformation = 0x5,
	SystemCallCountInformation = 0x6,
	SystemDeviceInformation = 0x7,
	SystemProcessorPerformanceInformation = 0x8,
	SystemFlagsInformation = 0x9,
	SystemCallTimeInformation = 0xa,
	SystemModuleInformation = 0xb,
	SystemLocksInformation = 0xc,
	SystemStackTraceInformation = 0xd,
	SystemPagedPoolInformation = 0xe,
	SystemNonPagedPoolInformation = 0xf,
	SystemHandleInformation = 0x10,
	SystemObjectInformation = 0x11,
	SystemPageFileInformation = 0x12,
	SystemVdmInstemulInformation = 0x13,
	SystemVdmBopInformation = 0x14,
	SystemFileCacheInformation = 0x15,
	SystemPoolTagInformation = 0x16,
	SystemInterruptInformation = 0x17,
	SystemDpcBehaviorInformation = 0x18,
	SystemLostDelayedWriteInformation = 0x19,
	SystemBigPoolInformation = 0x1a,
	SystemPrefetcherInformation = 0x1b,
	SystemExtendedProcessInformation = 0x1c,
	SystemFullMemoryInformation = 0x1d,
	SystemMirrorMemoryInformation = 0x1e,
	SystemObTraceInformation = 0x1f,
	SystemDebugPortInformation = 0x20,
	SystemLuidInformation = 0x21,
	SystemWow64SharedInformationObsolete = 0x22,
	SystemCallCountInformationEx = 0x23,
	SystemDeviceTreeInformation = 0x24,
	SystemSessionIdInformation = 0x25,
	SystemPhysicalMemoryInformation = 0x26,
	SystemHandleInformationEx = 0x27,
	SystemObjectInformationEx = 0x28,
	SystemBigPoolInformationEx = 0x29,
	SystemKernelVaSpaceInformation = 0x2a,
	SystemProcessorBrandString = 0x2b,
	SystemLogicalProcessorAndGroupInformation = 0x2c,
	SystemProcessorCycleTimeInformation = 0x2d,
	SystemStoreInformation = 0x2e,
	SystemPolicyInformation = 0x2f,
	SystemHypervisorInformation = 0x30,
	SystemPhysicalMemoryRuntimeInformation = 0x31,
	SystemSecureBootPolicyInformation = 0x32,
	SystemSecureBootInformation = 0x33,
	SystemCodeIntegrityInformation = 0x34,
	SystemProcessorMicrocodeUpdateInformation = 0x35,
	SystemProcessorFeatureInformation = 0x36,
	SystemProcessorMitigationsInformation = 0x37,
	SystemSpeculationControlInformation = 0x38,
	SystemDpcGuardInformation = 0x39,
	SystemSecureDumpPolicyInformation = 0x3a,
	SystemSecureDumpInformation = 0x3b,
	SystemLeakyAppInformation = 0x3c,
	SystemCpuSetInformation = 0x3d,
	SystemHypervisorSharedPageInformation = 0x3e,
	SystemNumaProximityNodeInformation = 0x3f,
	SystemLowPriorityIoInformation = 0x40,
	SystemTpmBootEntropyInformation = 0x41,
	SystemPaePageColorInformation = 0x42,
	SystemProcessorCycleTimeInformationEx = 0x43,
	SystemVerifierInformation = 0x44,
	SystemVerifierExhaustionInformation = 0x45,
	SystemPartitionInformation = 0x46,
	SystemSystemDependentRollbackInformation = 0x47,
	SystemNumaDistanceInformation = 0x48,
	SystemProcessorFirmwareInformation = 0x49,
	SystemProcessorIdleCycleTimeInformation = 0x4a,
	SystemVerifierCancellationInformation = 0x4b,
	SystemProcessorLoadInformation = 0x4c,
	SystemHardwareCounterInformation = 0x4d,
	SystemFirmwareTableInformation = 0x4e,
	SystemModuleInformationEx = 0x4f,
	SystemQuotaNonPagedPoolUsageInformation = 0x50,
	SystemInstallServiceStartOptionsInformation = 0x51,
	SystemBigPoolInformationShared = 0x52,
	SystemFilteringInformation = 0x53,
	SystemInterruptSteeringInformation = 0x54,
	SystemSecureKernelIoInformation = 0x55,
	SystemSystemPartitionInformation = 0x56,
	SystemSystemDiskInformation = 0x57,
	SystemProcessorPerformanceDistribution = 0x58,
	SystemNumaProximityNodeInformationEx = 0x59,
	SystemProcessorIdleCycleTimeInformationEx = 0x5a,
	SystemVerifierTripleFaultInformation = 0x5b,
	SystemCoverageInformation = 0x5c,
	SystemPrefetchPatchInformation = 0x5d,
	SystemVerifierFaultsInformation = 0x5e,
	SystemSystemPartitionInformationEx = 0x5f,
	SystemSecureBootPolicyFullInformation = 0x60,
	SystemSecureBootPolicyDetailedInformation = 0x61,
	SystemPrefetchersInformationEx = 0x62,
	SystemSecureBootBaseInformation = 0x63,
	SystemPoolZeroingInformation = 0x64,
	SystemDpcWatchdogInformation = 0x65,
	SystemSecureBootPolicyInformationEx = 0x66,
	SystemSecureBootSupplementalInformation = 0x67,
	SystemVsmProtectionInformation = 0x68,
	SystemHypervisorEnforcedCodeIntegrityInformation = 0x69,
	SystemHypervisorResourceInformation = 0x6a,
	SystemCrashDumpInformation = 0x6b,
	SystemPhysicalMemoryInformationEx = 0x6c,
	SystemMachineNameInformation = 0x6d,
	SystemDpcTimeoutWatchdogInformation = 0x6e,
	SystemLogicalProcessorInformationFromCet = 0x6f,
	SystemCetJumpTableInformation = 0x70,
	SystemDpcTimeoutWatchdogInformationV2 = 0x71,
	SystemProcessorGroupInformation = 0x72,
	SystemProcessorGroupInformationEx = 0x73,
	SystemProcessorCacheInformation = 0x74,
	SystemProcessorPackageInformation = 0x75,
	SystemProcessorDieInformation = 0x76,
	SystemProcessorNumaNodeInformation = 0x77,
	SystemProcessorSharedCacheInformation = 0x78,
	SystemNumaNodeRelationshipInformation = 0x79,
	SystemProcessorHierarchyInformation = 0x7a,
	SystemProcessorDistributionInformation = 0x7b,
	SystemNumaRelationshipInformation = 0x7c,
	SystemProcessorRelationshipInformation = 0x7d,
	SystemPreferredNodeInformation = 0x7e,
	SystemProcessorCycleTimeStatistics = 0x7f,
	SystemVsmProtectionInformationEx = 0x80,
	SystemCrashDumpHotInformation = 0x81,
	SystemProcessorIdleCycleTimeInformationEx = 0x82,
	SystemSecureBootPolicyInformationEx2 = 0x83,
	SystemProcessorCapabilitiesInformation = 0x84,
	SystemDpcWatchdogInformationEx = 0x85,
	SystemProcessorPerformanceInformationEx = 0x86,
	SystemProcessorPerformanceDistributionEx = 0x87,
	SystemProcessorIdleCycleTimeEx = 0x88,
	SystemProcessorPerformanceDistributionCheck = 0x89,
	SystemProcessorPerformanceDistributionEx2 = 0x8a,
	SystemProcessorPerformanceDistributionEx3 = 0x8b,
	SystemProcessorPerformanceDistributionEx4 = 0x8c,
	SystemProcessorPerformanceDistributionEx5 = 0x8d,
	SystemProcessorPerformanceDistributionEx6 = 0x8e,
	SystemProcessorPerformanceDistributionEx7 = 0x8f,
	SystemProcessorPerformanceDistributionEx8 = 0x90,
	SystemProcessorPerformanceDistributionEx9 = 0x91,
	SystemProcessorPerformanceDistributionEx10 = 0x92,
	SystemProcessorPerformanceDistributionEx11 = 0x93,
	SystemProcessorPerformanceDistributionEx12 = 0x94,
	SystemProcessorPerformanceDistributionEx13 = 0x95,
	SystemProcessorPerformanceDistributionEx14 = 0x96,
	SystemProcessorPerformanceDistributionEx15 = 0x97,
	SystemProcessorPerformanceDistributionEx16 = 0x98,
	SystemProcessorPerformanceDistributionEx17 = 0x99,
	SystemProcessorPerformanceDistributionEx18 = 0x9a,
	SystemProcessorPerformanceDistributionEx19 = 0x9b,
	SystemProcessorPerformanceDistributionEx20 = 0x9c,
	SystemProcessorPerformanceDistributionEx21 = 0x9d,
	SystemProcessorPerformanceDistributionEx22 = 0x9e,
	SystemProcessorPerformanceDistributionEx23 = 0x9f,
	SystemProcessorPerformanceDistributionEx24 = 0xa0,
	SystemProcessorPerformanceDistributionEx25 = 0xa1,
	SystemProcessorPerformanceDistributionEx26 = 0xa2,
	MaxSystemInfoClass = 0xa3,
} SYSTEM_INFORMATION_CLASS;
