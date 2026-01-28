# Contract: Registration-free COM Manifest Schema (operational)

Scope: Executables that activate COM must ship a manifest containing, at minimum:

- <assembly manifestVersion="1.0">
  - <file name="<NativeDll>"> elements for each native COM server
    - <comClass clsid="{GUID}" threadingModel="..." progid="..." tlbid="{GUID}" />
    - <typelib tlbid="{GUID}" version="*" helpdir="." />
    - <comInterfaceExternalProxyStub name="..." iid="{GUID}" proxyStubClsid32="{GUID}" />

Success criteria (operational): manifests include entries for all CLSIDs/IIDs used in primary flows and load successfully on clean machines.

Note: This contract documents expectations for generated content; the exact XML is produced by existing RegFree tooling.
