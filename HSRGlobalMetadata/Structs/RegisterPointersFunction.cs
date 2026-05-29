using HSRGlobalMetadata.Utils;

namespace HSRGlobalMetadata.Structs;

public class RegisterPointersFunction {
    public static RegisterPointersFunction? Instance;
    
    private string _gameAssemblyPath;
    public long FunctionOffset;
    public long FunctionRva;

    public RegisterPointersFunction(string gameAssemblyPath, long functionOffset, long functionRva) {
        _gameAssemblyPath = gameAssemblyPath;
        FunctionOffset = functionOffset;
        FunctionRva = functionRva;
    }

    public static RegisterPointersFunction Initialize(string gameAssemblyPath) {
        if (Instance == null) {
            var registerPointersFunctionOffset = PatternScanner.FindPatternInFile(gameAssemblyPath, "48 8D 05 ?? ?? ?? ?? 48 89 05 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 05 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 05 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 05");
            if (registerPointersFunctionOffset == -1)
                throw new Exception("Couldn't find pattern for registerPointersFunctionOffset");
            
            Instance = new RegisterPointersFunction(gameAssemblyPath, registerPointersFunctionOffset, PEHelper.OffsetToRva((uint)registerPointersFunctionOffset));
        }

        return Instance;
    }

    private long ReadRipRelativeOffset(int instructionOffset){
        var data = MetadataContext.Instance.GameAssembly;

        int baseOffset = (int)FunctionOffset + instructionOffset + 3;
        int disp = BitConverter.ToInt32(data, baseOffset);

        long nextInstructionRva = PEHelper.OffsetToRva((ulong)(FunctionOffset + instructionOffset + 7));
        return (long)PEHelper.RvaToOffset((uint)(nextInstructionRva + disp));
    }
    
    public long GetCodeRegistration() {
        return ReadRipRelativeOffset(0);
    }

    public long GetMetadataRegistration() {
        return ReadRipRelativeOffset(14);
    }

    public long GetMetadataTables() {
        return ReadRipRelativeOffset(28);
    }
}