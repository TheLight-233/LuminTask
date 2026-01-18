using System;
using System.Threading;

namespace LuminThread.Utility;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


/// <summary>
/// Cross-field, thread-safe high-performance bitmap with atomic Claim/Release, multi-word allocation and bitwise ops for concurrent memory/resource management.
/// Algorithms from: https://github.com/microsoft/mimalloc/blob/main/src/bitmap.h
/// </summary>
public sealed class LuminBitMap : IDisposable
{
    private static readonly int BITS_PER_FIELD = IntPtr.Size * 8;
        
    private nint[] _fields;
    private int _capacity;
    private int _fieldCount;
    private bool _isDisposed;

    public int Capacity => _capacity;
    public int FieldCount => _fieldCount;
    public bool IsCreated => !_isDisposed && _fields != null;

    public bool this[int bitIndex]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Get(bitIndex);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (value) Set(bitIndex);
            else Clear(bitIndex);
        }
    }

    #region Constructors
    public LuminBitMap(int bitCapacity)
    {
        if (bitCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(bitCapacity), "Bit capacity must be greater than zero");
            
        _capacity = bitCapacity;
        _fieldCount = (bitCapacity + BITS_PER_FIELD - 1) / BITS_PER_FIELD;
        _fields = new nint[_fieldCount];
        _isDisposed = false;
    }

    public LuminBitMap(scoped in LuminBitMap source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (!source.IsCreated)
            throw new ObjectDisposedException(nameof(LuminBitMap));
            
        _capacity = source._capacity;
        _fieldCount = source._fieldCount;
        _fields = new nint[_fieldCount];
        _isDisposed = false;
            
        // Copy fields
        Array.Copy(source._fields, _fields, _fieldCount);
    }
    #endregion

    #region Basic Bit Operations
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(int bitIndex)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        if (bitIndex < 0 || bitIndex >= _capacity)
            throw new ArgumentOutOfRangeException(nameof(bitIndex));

        int fieldIndex = bitIndex / BITS_PER_FIELD;
        int bitOffset = bitIndex % BITS_PER_FIELD;
            
        ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)fieldIndex);
        nint mask = (nint)1 << bitOffset;
            
        return (Volatile.Read(ref field) & mask) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int bitIndex)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        if (bitIndex < 0 || bitIndex >= _capacity)
            throw new ArgumentOutOfRangeException(nameof(bitIndex));

        int fieldIndex = bitIndex / BITS_PER_FIELD;
        int bitOffset = bitIndex % BITS_PER_FIELD;
            
        ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)fieldIndex);
        nint mask = (nint)1 << bitOffset;
            
        nint original, newValue;
        do
        {
            original = Volatile.Read(ref field);
            newValue = original | mask;
        } while (Interlocked.CompareExchange(ref field, newValue, original) != original);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(int bitIndex)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        if (bitIndex < 0 || bitIndex >= _capacity)
            throw new ArgumentOutOfRangeException(nameof(bitIndex));

        int fieldIndex = bitIndex / BITS_PER_FIELD;
        int bitOffset = bitIndex % BITS_PER_FIELD;
            
        ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)fieldIndex);
        nint mask = ~((nint)1 << bitOffset);
            
        nint original, newValue;
        do
        {
            original = Volatile.Read(ref field);
            newValue = original & mask;
        } while (Interlocked.CompareExchange(ref field, newValue, original) != original);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Toggle(int bitIndex)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        if (bitIndex < 0 || bitIndex >= _capacity)
            throw new ArgumentOutOfRangeException(nameof(bitIndex));

        int fieldIndex = bitIndex / BITS_PER_FIELD;
        int bitOffset = bitIndex % BITS_PER_FIELD;
            
        ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)fieldIndex);
        nint mask = (nint)1 << bitOffset;
            
        nint original, newValue;
        do
        {
            original = Volatile.Read(ref field);
            newValue = original ^ mask;
        } while (Interlocked.CompareExchange(ref field, newValue, original) != original);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAll()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        for (int i = 0; i < _fieldCount; i++)
        {
            ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)i);
            Volatile.Write(ref field, ~(nint)0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearAll()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        Array.Clear(_fields, 0, _fieldCount);
    }
    #endregion

    #region Claim Operations (Single Field)
    public bool TryFindAndClaim(int count, out BitmapIndex index)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        return TryFindAndClaimFrom(0, count, out index);
    }

    public bool TryFindAndClaimFrom(int startFieldIdx, int count, out BitmapIndex index)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        index = default;
            
        if (count <= 0 || count > BITS_PER_FIELD)
            return false;
            
        if (startFieldIdx < 0 || startFieldIdx >= _fieldCount)
            return false;

        nint targetMask = count == BITS_PER_FIELD ? ~(nint)0 : ((nint)1 << count) - 1;

        for (int fieldIdx = startFieldIdx; fieldIdx < _fieldCount; fieldIdx++)
        {
            ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)fieldIdx);
                
            for (int bitOffset = 0; bitOffset <= BITS_PER_FIELD - count; bitOffset++)
            {
                nint mask = targetMask << bitOffset;
                    
                nint original, newValue;
                do
                {
                    original = Volatile.Read(ref field);
                        
                    // Check if all bits are clear
                    if ((original & mask) != 0)
                        break;
                        
                    newValue = original | mask;
                } while (Interlocked.CompareExchange(ref field, newValue, original) != original);
                    
                // If we successfully set the bits
                if ((original & mask) == 0)
                {
                    index = new BitmapIndex(fieldIdx, bitOffset);
                    return true;
                }
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryClaim(BitmapIndex index, int count)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        if (count <= 0 || count > BITS_PER_FIELD)
            return false;
            
        if (index.FieldIndex < 0 || index.FieldIndex >= _fieldCount)
            return false;
            
        if (index.BitOffset < 0 || index.BitOffset + count > BITS_PER_FIELD)
            return false;

        nint targetMask = count == BITS_PER_FIELD ? ~(nint)0 : ((nint)1 << count) - 1;
        nint mask = targetMask << index.BitOffset;
            
        ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)index.FieldIndex);
            
        nint original, newValue;
        do
        {
            original = Volatile.Read(ref field);
                
            // Check if all bits are clear
            if ((original & mask) != 0)
                return false;
                
            newValue = original | mask;
        } while (Interlocked.CompareExchange(ref field, newValue, original) != original);
            
        return (original & mask) == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Claim(BitmapIndex index, int count, out bool anyZero)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        anyZero = false;
            
        if (count <= 0 || count > BITS_PER_FIELD)
            return false;
            
        if (index.FieldIndex < 0 || index.FieldIndex >= _fieldCount)
            return false;
            
        if (index.BitOffset < 0 || index.BitOffset + count > BITS_PER_FIELD)
            return false;

        nint targetMask = count == BITS_PER_FIELD ? ~(nint)0 : ((nint)1 << count) - 1;
        nint mask = targetMask << index.BitOffset;
            
        ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)index.FieldIndex);
            
        nint original, newValue;
        do
        {
            original = Volatile.Read(ref field);
            newValue = original | mask;
        } while (Interlocked.CompareExchange(ref field, newValue, original) != original);
            
        anyZero = (original & mask) != mask;
        return (original & mask) == mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Unclaim(BitmapIndex index, int count)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        if (count <= 0 || count > BITS_PER_FIELD)
            return false;
            
        if (index.FieldIndex < 0 || index.FieldIndex >= _fieldCount)
            return false;
            
        if (index.BitOffset < 0 || index.BitOffset + count > BITS_PER_FIELD)
            return false;

        nint targetMask = count == BITS_PER_FIELD ? ~(nint)0 : ((nint)1 << count) - 1;
        nint mask = ~(targetMask << index.BitOffset);
            
        ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)index.FieldIndex);
            
        nint original, newValue;
        do
        {
            original = Volatile.Read(ref field);
            newValue = original & mask;
        } while (Interlocked.CompareExchange(ref field, newValue, original) != original);
            
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsClaimed(BitmapIndex index, int count)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        if (count <= 0 || count > BITS_PER_FIELD)
            return false;
            
        if (index.FieldIndex < 0 || index.FieldIndex >= _fieldCount)
            return false;
            
        if (index.BitOffset < 0 || index.BitOffset + count > BITS_PER_FIELD)
            return false;

        nint targetMask = count == BITS_PER_FIELD ? ~(nint)0 : ((nint)1 << count) - 1;
        nint mask = targetMask << index.BitOffset;
            
        ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)index.FieldIndex);
        nint value = Volatile.Read(ref field);
            
        return (value & mask) == mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAnyClaimed(BitmapIndex index, int count)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        if (count <= 0 || count > BITS_PER_FIELD)
            return false;
            
        if (index.FieldIndex < 0 || index.FieldIndex >= _fieldCount)
            return false;
            
        if (index.BitOffset < 0 || index.BitOffset + count > BITS_PER_FIELD)
            return false;

        nint targetMask = count == BITS_PER_FIELD ? ~(nint)0 : ((nint)1 << count) - 1;
        nint mask = targetMask << index.BitOffset;
            
        ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)index.FieldIndex);
        nint value = Volatile.Read(ref field);
            
        return (value & mask) != 0;
    }
    #endregion

    #region Claim Operations (Across Fields)
    public bool TryFindAndClaimAcross(int count, out BitmapIndex index)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        return TryFindAndClaimAcrossFrom(0, count, out index);
    }

    public bool TryFindAndClaimAcrossFrom(int startFieldIdx, int count, out BitmapIndex index)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        index = default;
            
        if (count <= 0 || count > _capacity)
            return false;
            
        if (startFieldIdx < 0 || startFieldIdx >= _fieldCount)
            return false;

        int totalBitsToCheck = _capacity - (startFieldIdx * BITS_PER_FIELD);
        if (count > totalBitsToCheck)
            return false;

        // Try to find contiguous clear bits across fields
        for (int fieldIdx = startFieldIdx; fieldIdx < _fieldCount; fieldIdx++)
        {
            for (int bitOffset = 0; bitOffset < BITS_PER_FIELD; bitOffset++)
            {
                int startBitIndex = fieldIdx * BITS_PER_FIELD + bitOffset;
                if (startBitIndex + count > _capacity)
                    break;

                // Check if we can claim from this position
                bool canClaim = true;
                int remainingCount = count;
                int currentFieldIdx = fieldIdx;
                int currentBitOffset = bitOffset;

                // First pass: check if all bits are clear
                while (remainingCount > 0 && canClaim)
                {
                    int bitsInCurrentField = Math.Min(remainingCount, BITS_PER_FIELD - currentBitOffset);
                        
                    ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)currentFieldIdx);
                    nint fieldValue = Volatile.Read(ref field);
                        
                    nint targetMask = bitsInCurrentField == BITS_PER_FIELD ? ~(nint)0 : ((nint)1 << bitsInCurrentField) - 1;
                    nint mask = targetMask << currentBitOffset;
                        
                    if ((fieldValue & mask) != 0)
                    {
                        canClaim = false;
                        break;
                    }
                        
                    remainingCount -= bitsInCurrentField;
                    currentFieldIdx++;
                    currentBitOffset = 0;
                }

                if (!canClaim)
                    continue;

                // Second pass: try to claim atomically
                remainingCount = count;
                currentFieldIdx = fieldIdx;
                currentBitOffset = bitOffset;
                bool allClaimed = true;

                while (remainingCount > 0 && allClaimed)
                {
                    int bitsInCurrentField = Math.Min(remainingCount, BITS_PER_FIELD - currentBitOffset);
                        
                    ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)currentFieldIdx);
                        
                    nint targetMask = bitsInCurrentField == BITS_PER_FIELD ? ~(nint)0 : ((nint)1 << bitsInCurrentField) - 1;
                    nint mask = targetMask << currentBitOffset;
                        
                    nint original, newValue;
                    do
                    {
                        original = Volatile.Read(ref field);
                            
                        if ((original & mask) != 0)
                        {
                            allClaimed = false;
                            break;
                        }
                            
                        newValue = original | mask;
                    } while (Interlocked.CompareExchange(ref field, newValue, original) != original);
                        
                    if (!allClaimed || (original & mask) != 0)
                    {
                        allClaimed = false;
                            
                        // Rollback previous claims
                        int rollbackFieldIdx = fieldIdx;
                        int rollbackBitOffset = bitOffset;
                        int rollbackCount = count - remainingCount;
                            
                        while (rollbackCount > 0)
                        {
                            int rollbackBits = Math.Min(rollbackCount, BITS_PER_FIELD - rollbackBitOffset);
                                
                            ref nint rollbackField = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)rollbackFieldIdx);
                                
                            nint rollbackTargetMask = rollbackBits == BITS_PER_FIELD ? ~(nint)0 : ((nint)1 << rollbackBits) - 1;
                            nint rollbackMask = ~(rollbackTargetMask << rollbackBitOffset);
                                
                            nint rollbackOriginal, rollbackNewValue;
                            do
                            {
                                rollbackOriginal = Volatile.Read(ref rollbackField);
                                rollbackNewValue = rollbackOriginal & rollbackMask;
                            } while (Interlocked.CompareExchange(ref rollbackField, rollbackNewValue, rollbackOriginal) != rollbackOriginal);
                                
                            rollbackCount -= rollbackBits;
                            rollbackFieldIdx++;
                            rollbackBitOffset = 0;
                        }
                            
                        break;
                    }
                        
                    remainingCount -= bitsInCurrentField;
                    currentFieldIdx++;
                    currentBitOffset = 0;
                }

                if (allClaimed)
                {
                    index = new BitmapIndex(fieldIdx, bitOffset);
                    return true;
                }
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ClaimAcross(BitmapIndex index, int count, out bool anyZero, out int alreadySet)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        anyZero = false;
        alreadySet = 0;
            
        if (count <= 0)
            return false;
            
        if (index.FieldIndex < 0 || index.FieldIndex >= _fieldCount)
            return false;
            
        int startBitIndex = index.FieldIndex * BITS_PER_FIELD + index.BitOffset;
        if (startBitIndex + count > _capacity)
            return false;

        int remainingCount = count;
        int currentFieldIdx = index.FieldIndex;
        int currentBitOffset = index.BitOffset;
        bool allWereSet = true;

        while (remainingCount > 0)
        {
            int bitsInCurrentField = Math.Min(remainingCount, BITS_PER_FIELD - currentBitOffset);
                
            ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)currentFieldIdx);
                
            nint targetMask = bitsInCurrentField == BITS_PER_FIELD ? ~(nint)0 : ((nint)1 << bitsInCurrentField) - 1;
            nint mask = targetMask << currentBitOffset;
                
            nint original, newValue;
            do
            {
                original = Volatile.Read(ref field);
                newValue = original | mask;
            } while (Interlocked.CompareExchange(ref field, newValue, original) != original);
                
            nint setBits = original & mask;
            if (setBits != mask)
            {
                allWereSet = false;
                anyZero = true;
            }
                
            // Count already set bits
            nint temp = setBits;
            while (temp != 0)
            {
                alreadySet++;
                temp &= temp - 1;
            }
                
            remainingCount -= bitsInCurrentField;
            currentFieldIdx++;
            currentBitOffset = 0;
        }

        return allWereSet;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool UnclaimAcross(BitmapIndex index, int count)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        if (count <= 0)
            return false;
            
        if (index.FieldIndex < 0 || index.FieldIndex >= _fieldCount)
            return false;
            
        int startBitIndex = index.FieldIndex * BITS_PER_FIELD + index.BitOffset;
        if (startBitIndex + count > _capacity)
            return false;

        int remainingCount = count;
        int currentFieldIdx = index.FieldIndex;
        int currentBitOffset = index.BitOffset;

        while (remainingCount > 0)
        {
            int bitsInCurrentField = Math.Min(remainingCount, BITS_PER_FIELD - currentBitOffset);
                
            ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)currentFieldIdx);
                
            nint targetMask = bitsInCurrentField == BITS_PER_FIELD ? ~(nint)0 : ((nint)1 << bitsInCurrentField) - 1;
            nint mask = ~(targetMask << currentBitOffset);
                
            nint original, newValue;
            do
            {
                original = Volatile.Read(ref field);
                newValue = original & mask;
            } while (Interlocked.CompareExchange(ref field, newValue, original) != original);
                
            remainingCount -= bitsInCurrentField;
            currentFieldIdx++;
            currentBitOffset = 0;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsClaimedAcross(BitmapIndex index, int count, out int alreadySet)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        alreadySet = 0;
            
        if (count <= 0)
            return false;
            
        if (index.FieldIndex < 0 || index.FieldIndex >= _fieldCount)
            return false;
            
        int startBitIndex = index.FieldIndex * BITS_PER_FIELD + index.BitOffset;
        if (startBitIndex + count > _capacity)
            return false;

        int remainingCount = count;
        int currentFieldIdx = index.FieldIndex;
        int currentBitOffset = index.BitOffset;
        bool allSet = true;

        while (remainingCount > 0)
        {
            int bitsInCurrentField = Math.Min(remainingCount, BITS_PER_FIELD - currentBitOffset);
                
            ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)currentFieldIdx);
            nint fieldValue = Volatile.Read(ref field);
                
            nint targetMask = bitsInCurrentField == BITS_PER_FIELD ? ~(nint)0 : ((nint)1 << bitsInCurrentField) - 1;
            nint mask = targetMask << currentBitOffset;
                
            nint setBits = fieldValue & mask;
            if (setBits != mask)
                allSet = false;
                
            // Count set bits
            nint temp = setBits;
            while (temp != 0)
            {
                alreadySet++;
                temp &= temp - 1;
            }
                
            remainingCount -= bitsInCurrentField;
            currentFieldIdx++;
            currentBitOffset = 0;
        }

        return allSet;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAnyClaimedAcross(BitmapIndex index, int count)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        if (count <= 0)
            return false;
            
        if (index.FieldIndex < 0 || index.FieldIndex >= _fieldCount)
            return false;
            
        int startBitIndex = index.FieldIndex * BITS_PER_FIELD + index.BitOffset;
        if (startBitIndex + count > _capacity)
            return false;

        int remainingCount = count;
        int currentFieldIdx = index.FieldIndex;
        int currentBitOffset = index.BitOffset;

        while (remainingCount > 0)
        {
            int bitsInCurrentField = Math.Min(remainingCount, BITS_PER_FIELD - currentBitOffset);
                
            ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)currentFieldIdx);
            nint fieldValue = Volatile.Read(ref field);
                
            nint targetMask = bitsInCurrentField == BITS_PER_FIELD ? ~(nint)0 : ((nint)1 << bitsInCurrentField) - 1;
            nint mask = targetMask << currentBitOffset;
                
            if ((fieldValue & mask) != 0)
                return true;
                
            remainingCount -= bitsInCurrentField;
            currentFieldIdx++;
            currentBitOffset = 0;
        }

        return false;
    }
    #endregion

    #region Query Operations
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CountSetBits()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        int count = 0;
            
        for (int i = 0; i < _fieldCount; i++)
        {
            ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)i);
            nint value = Volatile.Read(ref field);
                
            // Population count (Hamming weight)
            nint temp = value;
            while (temp != 0)
            {
                count++;
                temp &= temp - 1;
            }
        }
            
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CountClearBits()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        return _capacity - CountSetBits();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Any()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        for (int i = 0; i < _fieldCount; i++)
        {
            ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)i);
            if (Volatile.Read(ref field) != IntPtr.Zero)
                return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool All()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        // Check all complete fields
        for (int i = 0; i < _fieldCount - 1; i++)
        {
            ref nint field = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)i);
            if (Volatile.Read(ref field) != ~(nint)0)
                return false;
        }
            
        // Check last field with proper masking
        if (_fieldCount > 0)
        {
            int lastFieldBits = _capacity % BITS_PER_FIELD;
            if (lastFieldBits == 0)
                lastFieldBits = BITS_PER_FIELD;
                
            nint lastFieldMask = lastFieldBits == BITS_PER_FIELD ? ~(nint)0 : ((nint)1 << lastFieldBits) - 1;
                
            ref nint lastField = ref Unsafe.Add(ref MemoryMarshal.GetReference(_fields.AsSpan()), (nint)(_fieldCount - 1));
            nint lastFieldValue = Volatile.Read(ref lastField);
                
            if ((lastFieldValue & lastFieldMask) != lastFieldMask)
                return false;
        }
            
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool None()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        return !Any();
    }
    #endregion

    #region Span Operations
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<nint> AsSpan()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        return _fields.AsSpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<nint> AsReadOnlySpan()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        return _fields.AsSpan();
    }
    #endregion

    #region Conversion and Utility
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool[] ToBoolArray()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
            
        bool[] result = new bool[_capacity];
        for (int i = 0; i < _capacity; i++)
        {
            result[i] = Get(i);
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(bool[] array, int arrayIndex)
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        if (array is null)
            throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        if (arrayIndex + _capacity > array.Length)
            throw new ArgumentException("Destination array is not long enough");
            
        for (int i = 0; i < _capacity; i++)
        {
            array[arrayIndex + i] = Get(i);
        }
    }
    #endregion

    #region Enumerator
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!IsCreated, this);
#else
            if (!IsCreated) 
                throw new ObjectDisposedException(nameof(LuminBitMap));
#endif
        return new Enumerator(this);
    }

    public struct Enumerator
    {
        private readonly LuminBitMap _bitMap;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(LuminBitMap bitMap)
        {
            _bitMap = bitMap;
            _index = -1;
        }

        public bool Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _bitMap.Get(_index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int newIndex = _index + 1;
            if (newIndex < _bitMap._capacity)
            {
                _index = newIndex;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => _index = -1;
    }
    #endregion

    #region BitmapIndex Struct
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BitmapIndex : IEquatable<BitmapIndex>
    {
        public readonly int FieldIndex;
        public readonly int BitOffset;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitmapIndex(int fieldIndex, int bitOffset)
        {
            FieldIndex = fieldIndex;
            BitOffset = bitOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToAbsoluteIndex() => FieldIndex * BITS_PER_FIELD + BitOffset;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitmapIndex FromAbsoluteIndex(int absoluteIndex)
        {
            int fieldIndex = absoluteIndex / BITS_PER_FIELD;
            int bitOffset = absoluteIndex % BITS_PER_FIELD;
            return new BitmapIndex(fieldIndex, bitOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(BitmapIndex other)
        {
            return FieldIndex == other.FieldIndex && BitOffset == other.BitOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj)
        {
            return obj is BitmapIndex other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode.Combine(FieldIndex, BitOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(BitmapIndex left, BitmapIndex right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(BitmapIndex left, BitmapIndex right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"BitmapIndex(Field: {FieldIndex}, Offset: {BitOffset})";
        }
    }
    #endregion

    #region Dispose
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _fields = null!;
        }
    }
    #endregion
}