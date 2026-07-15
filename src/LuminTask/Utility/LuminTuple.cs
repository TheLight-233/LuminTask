
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace LuminThread.Utility
{

    [Serializable]
    public struct LuminTuple<T1>
    {
        private readonly T1 m_Item1;

        public LuminTuple(T1 item1)
        {
            m_Item1 = item1;
        }

        public static LuminTuple<T1> Create(T1 item1)
        {
            return new LuminTuple<T1>(item1);
        }

        public T1 Item1 => m_Item1;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1})";
        }

        public void Deconstruct(out T1 item1)
        {
            item1 = m_Item1;
        }

        public static bool operator ==(LuminTuple<T1> left, LuminTuple<T1> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1> left, LuminTuple<T1> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;

        public LuminTuple(T1 item1, T2 item2)
        {
            m_Item1 = item1;
            m_Item2 = item2;
        }

        public static LuminTuple<T1, T2> Create(T1 item1, T2 item2)
        {
            return new LuminTuple<T1, T2>(item1, item2);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2})";
        }

        public void Deconstruct(out T1 item1, out T2 item2)
        {
            item1 = m_Item1;
            item2 = m_Item2;
        }

        public static bool operator ==(LuminTuple<T1, T2> left, LuminTuple<T1, T2> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2> left, LuminTuple<T1, T2> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2, T3>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;

        public LuminTuple(T1 item1, T2 item2, T3 item3)
        {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
        }

        public static LuminTuple<T1, T2, T3> Create(T1 item1, T2 item2, T3 item3)
        {
            return new LuminTuple<T1, T2, T3>(item1, item2, item3);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public T3 Item3 => m_Item3;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2, T3> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2, T3> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
                && EqualityComparer<T3>.Default.Equals(m_Item3, other.m_Item3)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item3?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2}, {Item3})";
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3)
        {
            item1 = m_Item1;
            item2 = m_Item2;
            item3 = m_Item3;
        }

        public static bool operator ==(LuminTuple<T1, T2, T3> left, LuminTuple<T1, T2, T3> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2, T3> left, LuminTuple<T1, T2, T3> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2, T3, T4>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;

        public LuminTuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
        }

        public static LuminTuple<T1, T2, T3, T4> Create(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            return new LuminTuple<T1, T2, T3, T4>(item1, item2, item3, item4);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public T3 Item3 => m_Item3;

        public T4 Item4 => m_Item4;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2, T3, T4> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2, T3, T4> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
                && EqualityComparer<T3>.Default.Equals(m_Item3, other.m_Item3)
                && EqualityComparer<T4>.Default.Equals(m_Item4, other.m_Item4)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item3?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item4?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2}, {Item3}, {Item4})";
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4)
        {
            item1 = m_Item1;
            item2 = m_Item2;
            item3 = m_Item3;
            item4 = m_Item4;
        }

        public static bool operator ==(LuminTuple<T1, T2, T3, T4> left, LuminTuple<T1, T2, T3, T4> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2, T3, T4> left, LuminTuple<T1, T2, T3, T4> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2, T3, T4, T5>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;

        public LuminTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
        }

        public static LuminTuple<T1, T2, T3, T4, T5> Create(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            return new LuminTuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public T3 Item3 => m_Item3;

        public T4 Item4 => m_Item4;

        public T5 Item5 => m_Item5;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2, T3, T4, T5> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2, T3, T4, T5> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
                && EqualityComparer<T3>.Default.Equals(m_Item3, other.m_Item3)
                && EqualityComparer<T4>.Default.Equals(m_Item4, other.m_Item4)
                && EqualityComparer<T5>.Default.Equals(m_Item5, other.m_Item5)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item3?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item4?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item5?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2}, {Item3}, {Item4}, {Item5})";
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5)
        {
            item1 = m_Item1;
            item2 = m_Item2;
            item3 = m_Item3;
            item4 = m_Item4;
            item5 = m_Item5;
        }

        public static bool operator ==(LuminTuple<T1, T2, T3, T4, T5> left, LuminTuple<T1, T2, T3, T4, T5> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2, T3, T4, T5> left, LuminTuple<T1, T2, T3, T4, T5> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2, T3, T4, T5, T6>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;
        private readonly T6 m_Item6;

        public LuminTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
            m_Item6 = item6;
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6> Create(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public T3 Item3 => m_Item3;

        public T4 Item4 => m_Item4;

        public T5 Item5 => m_Item5;

        public T6 Item6 => m_Item6;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2, T3, T4, T5, T6> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2, T3, T4, T5, T6> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
                && EqualityComparer<T3>.Default.Equals(m_Item3, other.m_Item3)
                && EqualityComparer<T4>.Default.Equals(m_Item4, other.m_Item4)
                && EqualityComparer<T5>.Default.Equals(m_Item5, other.m_Item5)
                && EqualityComparer<T6>.Default.Equals(m_Item6, other.m_Item6)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item3?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item4?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item5?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item6?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2}, {Item3}, {Item4}, {Item5}, {Item6})";
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6)
        {
            item1 = m_Item1;
            item2 = m_Item2;
            item3 = m_Item3;
            item4 = m_Item4;
            item5 = m_Item5;
            item6 = m_Item6;
        }

        public static bool operator ==(LuminTuple<T1, T2, T3, T4, T5, T6> left, LuminTuple<T1, T2, T3, T4, T5, T6> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2, T3, T4, T5, T6> left, LuminTuple<T1, T2, T3, T4, T5, T6> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2, T3, T4, T5, T6, T7>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;
        private readonly T6 m_Item6;
        private readonly T7 m_Item7;

        public LuminTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
            m_Item6 = item6;
            m_Item7 = item7;
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7> Create(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public T3 Item3 => m_Item3;

        public T4 Item4 => m_Item4;

        public T5 Item5 => m_Item5;

        public T6 Item6 => m_Item6;

        public T7 Item7 => m_Item7;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2, T3, T4, T5, T6, T7> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2, T3, T4, T5, T6, T7> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
                && EqualityComparer<T3>.Default.Equals(m_Item3, other.m_Item3)
                && EqualityComparer<T4>.Default.Equals(m_Item4, other.m_Item4)
                && EqualityComparer<T5>.Default.Equals(m_Item5, other.m_Item5)
                && EqualityComparer<T6>.Default.Equals(m_Item6, other.m_Item6)
                && EqualityComparer<T7>.Default.Equals(m_Item7, other.m_Item7)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item3?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item4?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item5?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item6?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item7?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2}, {Item3}, {Item4}, {Item5}, {Item6}, {Item7})";
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7)
        {
            item1 = m_Item1;
            item2 = m_Item2;
            item3 = m_Item3;
            item4 = m_Item4;
            item5 = m_Item5;
            item6 = m_Item6;
            item7 = m_Item7;
        }

        public static bool operator ==(LuminTuple<T1, T2, T3, T4, T5, T6, T7> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2, T3, T4, T5, T6, T7> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;
        private readonly T6 m_Item6;
        private readonly T7 m_Item7;
        private readonly T8 m_Item8;

        public LuminTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
            m_Item6 = item6;
            m_Item7 = item7;
            m_Item8 = item8;
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8> Create(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8>(item1, item2, item3, item4, item5, item6, item7, item8);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public T3 Item3 => m_Item3;

        public T4 Item4 => m_Item4;

        public T5 Item5 => m_Item5;

        public T6 Item6 => m_Item6;

        public T7 Item7 => m_Item7;

        public T8 Item8 => m_Item8;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
                && EqualityComparer<T3>.Default.Equals(m_Item3, other.m_Item3)
                && EqualityComparer<T4>.Default.Equals(m_Item4, other.m_Item4)
                && EqualityComparer<T5>.Default.Equals(m_Item5, other.m_Item5)
                && EqualityComparer<T6>.Default.Equals(m_Item6, other.m_Item6)
                && EqualityComparer<T7>.Default.Equals(m_Item7, other.m_Item7)
                && EqualityComparer<T8>.Default.Equals(m_Item8, other.m_Item8)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item3?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item4?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item5?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item6?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item7?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item8?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2}, {Item3}, {Item4}, {Item5}, {Item6}, {Item7}, {Item8})";
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8)
        {
            item1 = m_Item1;
            item2 = m_Item2;
            item3 = m_Item3;
            item4 = m_Item4;
            item5 = m_Item5;
            item6 = m_Item6;
            item7 = m_Item7;
            item8 = m_Item8;
        }

        public static bool operator ==(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;
        private readonly T6 m_Item6;
        private readonly T7 m_Item7;
        private readonly T8 m_Item8;
        private readonly T9 m_Item9;

        public LuminTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
        {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
            m_Item6 = item6;
            m_Item7 = item7;
            m_Item8 = item8;
            m_Item9 = item9;
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> Create(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>(item1, item2, item3, item4, item5, item6, item7, item8, item9);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public T3 Item3 => m_Item3;

        public T4 Item4 => m_Item4;

        public T5 Item5 => m_Item5;

        public T6 Item6 => m_Item6;

        public T7 Item7 => m_Item7;

        public T8 Item8 => m_Item8;

        public T9 Item9 => m_Item9;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
                && EqualityComparer<T3>.Default.Equals(m_Item3, other.m_Item3)
                && EqualityComparer<T4>.Default.Equals(m_Item4, other.m_Item4)
                && EqualityComparer<T5>.Default.Equals(m_Item5, other.m_Item5)
                && EqualityComparer<T6>.Default.Equals(m_Item6, other.m_Item6)
                && EqualityComparer<T7>.Default.Equals(m_Item7, other.m_Item7)
                && EqualityComparer<T8>.Default.Equals(m_Item8, other.m_Item8)
                && EqualityComparer<T9>.Default.Equals(m_Item9, other.m_Item9)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item3?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item4?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item5?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item6?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item7?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item8?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item9?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2}, {Item3}, {Item4}, {Item5}, {Item6}, {Item7}, {Item8}, {Item9})";
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9)
        {
            item1 = m_Item1;
            item2 = m_Item2;
            item3 = m_Item3;
            item4 = m_Item4;
            item5 = m_Item5;
            item6 = m_Item6;
            item7 = m_Item7;
            item8 = m_Item8;
            item9 = m_Item9;
        }

        public static bool operator ==(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;
        private readonly T6 m_Item6;
        private readonly T7 m_Item7;
        private readonly T8 m_Item8;
        private readonly T9 m_Item9;
        private readonly T10 m_Item10;

        public LuminTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10)
        {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
            m_Item6 = item6;
            m_Item7 = item7;
            m_Item8 = item8;
            m_Item9 = item9;
            m_Item10 = item10;
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Create(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public T3 Item3 => m_Item3;

        public T4 Item4 => m_Item4;

        public T5 Item5 => m_Item5;

        public T6 Item6 => m_Item6;

        public T7 Item7 => m_Item7;

        public T8 Item8 => m_Item8;

        public T9 Item9 => m_Item9;

        public T10 Item10 => m_Item10;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
                && EqualityComparer<T3>.Default.Equals(m_Item3, other.m_Item3)
                && EqualityComparer<T4>.Default.Equals(m_Item4, other.m_Item4)
                && EqualityComparer<T5>.Default.Equals(m_Item5, other.m_Item5)
                && EqualityComparer<T6>.Default.Equals(m_Item6, other.m_Item6)
                && EqualityComparer<T7>.Default.Equals(m_Item7, other.m_Item7)
                && EqualityComparer<T8>.Default.Equals(m_Item8, other.m_Item8)
                && EqualityComparer<T9>.Default.Equals(m_Item9, other.m_Item9)
                && EqualityComparer<T10>.Default.Equals(m_Item10, other.m_Item10)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item3?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item4?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item5?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item6?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item7?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item8?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item9?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item10?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2}, {Item3}, {Item4}, {Item5}, {Item6}, {Item7}, {Item8}, {Item9}, {Item10})";
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10)
        {
            item1 = m_Item1;
            item2 = m_Item2;
            item3 = m_Item3;
            item4 = m_Item4;
            item5 = m_Item5;
            item6 = m_Item6;
            item7 = m_Item7;
            item8 = m_Item8;
            item9 = m_Item9;
            item10 = m_Item10;
        }

        public static bool operator ==(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;
        private readonly T6 m_Item6;
        private readonly T7 m_Item7;
        private readonly T8 m_Item8;
        private readonly T9 m_Item9;
        private readonly T10 m_Item10;
        private readonly T11 m_Item11;

        public LuminTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11)
        {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
            m_Item6 = item6;
            m_Item7 = item7;
            m_Item8 = item8;
            m_Item9 = item9;
            m_Item10 = item10;
            m_Item11 = item11;
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Create(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public T3 Item3 => m_Item3;

        public T4 Item4 => m_Item4;

        public T5 Item5 => m_Item5;

        public T6 Item6 => m_Item6;

        public T7 Item7 => m_Item7;

        public T8 Item8 => m_Item8;

        public T9 Item9 => m_Item9;

        public T10 Item10 => m_Item10;

        public T11 Item11 => m_Item11;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
                && EqualityComparer<T3>.Default.Equals(m_Item3, other.m_Item3)
                && EqualityComparer<T4>.Default.Equals(m_Item4, other.m_Item4)
                && EqualityComparer<T5>.Default.Equals(m_Item5, other.m_Item5)
                && EqualityComparer<T6>.Default.Equals(m_Item6, other.m_Item6)
                && EqualityComparer<T7>.Default.Equals(m_Item7, other.m_Item7)
                && EqualityComparer<T8>.Default.Equals(m_Item8, other.m_Item8)
                && EqualityComparer<T9>.Default.Equals(m_Item9, other.m_Item9)
                && EqualityComparer<T10>.Default.Equals(m_Item10, other.m_Item10)
                && EqualityComparer<T11>.Default.Equals(m_Item11, other.m_Item11)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item3?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item4?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item5?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item6?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item7?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item8?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item9?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item10?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item11?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2}, {Item3}, {Item4}, {Item5}, {Item6}, {Item7}, {Item8}, {Item9}, {Item10}, {Item11})";
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11)
        {
            item1 = m_Item1;
            item2 = m_Item2;
            item3 = m_Item3;
            item4 = m_Item4;
            item5 = m_Item5;
            item6 = m_Item6;
            item7 = m_Item7;
            item8 = m_Item8;
            item9 = m_Item9;
            item10 = m_Item10;
            item11 = m_Item11;
        }

        public static bool operator ==(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;
        private readonly T6 m_Item6;
        private readonly T7 m_Item7;
        private readonly T8 m_Item8;
        private readonly T9 m_Item9;
        private readonly T10 m_Item10;
        private readonly T11 m_Item11;
        private readonly T12 m_Item12;

        public LuminTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12)
        {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
            m_Item6 = item6;
            m_Item7 = item7;
            m_Item8 = item8;
            m_Item9 = item9;
            m_Item10 = item10;
            m_Item11 = item11;
            m_Item12 = item12;
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Create(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public T3 Item3 => m_Item3;

        public T4 Item4 => m_Item4;

        public T5 Item5 => m_Item5;

        public T6 Item6 => m_Item6;

        public T7 Item7 => m_Item7;

        public T8 Item8 => m_Item8;

        public T9 Item9 => m_Item9;

        public T10 Item10 => m_Item10;

        public T11 Item11 => m_Item11;

        public T12 Item12 => m_Item12;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
                && EqualityComparer<T3>.Default.Equals(m_Item3, other.m_Item3)
                && EqualityComparer<T4>.Default.Equals(m_Item4, other.m_Item4)
                && EqualityComparer<T5>.Default.Equals(m_Item5, other.m_Item5)
                && EqualityComparer<T6>.Default.Equals(m_Item6, other.m_Item6)
                && EqualityComparer<T7>.Default.Equals(m_Item7, other.m_Item7)
                && EqualityComparer<T8>.Default.Equals(m_Item8, other.m_Item8)
                && EqualityComparer<T9>.Default.Equals(m_Item9, other.m_Item9)
                && EqualityComparer<T10>.Default.Equals(m_Item10, other.m_Item10)
                && EqualityComparer<T11>.Default.Equals(m_Item11, other.m_Item11)
                && EqualityComparer<T12>.Default.Equals(m_Item12, other.m_Item12)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item3?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item4?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item5?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item6?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item7?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item8?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item9?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item10?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item11?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item12?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2}, {Item3}, {Item4}, {Item5}, {Item6}, {Item7}, {Item8}, {Item9}, {Item10}, {Item11}, {Item12})";
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12)
        {
            item1 = m_Item1;
            item2 = m_Item2;
            item3 = m_Item3;
            item4 = m_Item4;
            item5 = m_Item5;
            item6 = m_Item6;
            item7 = m_Item7;
            item8 = m_Item8;
            item9 = m_Item9;
            item10 = m_Item10;
            item11 = m_Item11;
            item12 = m_Item12;
        }

        public static bool operator ==(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;
        private readonly T6 m_Item6;
        private readonly T7 m_Item7;
        private readonly T8 m_Item8;
        private readonly T9 m_Item9;
        private readonly T10 m_Item10;
        private readonly T11 m_Item11;
        private readonly T12 m_Item12;
        private readonly T13 m_Item13;

        public LuminTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13)
        {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
            m_Item6 = item6;
            m_Item7 = item7;
            m_Item8 = item8;
            m_Item9 = item9;
            m_Item10 = item10;
            m_Item11 = item11;
            m_Item12 = item12;
            m_Item13 = item13;
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Create(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public T3 Item3 => m_Item3;

        public T4 Item4 => m_Item4;

        public T5 Item5 => m_Item5;

        public T6 Item6 => m_Item6;

        public T7 Item7 => m_Item7;

        public T8 Item8 => m_Item8;

        public T9 Item9 => m_Item9;

        public T10 Item10 => m_Item10;

        public T11 Item11 => m_Item11;

        public T12 Item12 => m_Item12;

        public T13 Item13 => m_Item13;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
                && EqualityComparer<T3>.Default.Equals(m_Item3, other.m_Item3)
                && EqualityComparer<T4>.Default.Equals(m_Item4, other.m_Item4)
                && EqualityComparer<T5>.Default.Equals(m_Item5, other.m_Item5)
                && EqualityComparer<T6>.Default.Equals(m_Item6, other.m_Item6)
                && EqualityComparer<T7>.Default.Equals(m_Item7, other.m_Item7)
                && EqualityComparer<T8>.Default.Equals(m_Item8, other.m_Item8)
                && EqualityComparer<T9>.Default.Equals(m_Item9, other.m_Item9)
                && EqualityComparer<T10>.Default.Equals(m_Item10, other.m_Item10)
                && EqualityComparer<T11>.Default.Equals(m_Item11, other.m_Item11)
                && EqualityComparer<T12>.Default.Equals(m_Item12, other.m_Item12)
                && EqualityComparer<T13>.Default.Equals(m_Item13, other.m_Item13)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item3?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item4?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item5?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item6?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item7?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item8?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item9?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item10?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item11?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item12?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item13?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2}, {Item3}, {Item4}, {Item5}, {Item6}, {Item7}, {Item8}, {Item9}, {Item10}, {Item11}, {Item12}, {Item13})";
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13)
        {
            item1 = m_Item1;
            item2 = m_Item2;
            item3 = m_Item3;
            item4 = m_Item4;
            item5 = m_Item5;
            item6 = m_Item6;
            item7 = m_Item7;
            item8 = m_Item8;
            item9 = m_Item9;
            item10 = m_Item10;
            item11 = m_Item11;
            item12 = m_Item12;
            item13 = m_Item13;
        }

        public static bool operator ==(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;
        private readonly T6 m_Item6;
        private readonly T7 m_Item7;
        private readonly T8 m_Item8;
        private readonly T9 m_Item9;
        private readonly T10 m_Item10;
        private readonly T11 m_Item11;
        private readonly T12 m_Item12;
        private readonly T13 m_Item13;
        private readonly T14 m_Item14;

        public LuminTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14)
        {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
            m_Item6 = item6;
            m_Item7 = item7;
            m_Item8 = item8;
            m_Item9 = item9;
            m_Item10 = item10;
            m_Item11 = item11;
            m_Item12 = item12;
            m_Item13 = item13;
            m_Item14 = item14;
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Create(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public T3 Item3 => m_Item3;

        public T4 Item4 => m_Item4;

        public T5 Item5 => m_Item5;

        public T6 Item6 => m_Item6;

        public T7 Item7 => m_Item7;

        public T8 Item8 => m_Item8;

        public T9 Item9 => m_Item9;

        public T10 Item10 => m_Item10;

        public T11 Item11 => m_Item11;

        public T12 Item12 => m_Item12;

        public T13 Item13 => m_Item13;

        public T14 Item14 => m_Item14;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
                && EqualityComparer<T3>.Default.Equals(m_Item3, other.m_Item3)
                && EqualityComparer<T4>.Default.Equals(m_Item4, other.m_Item4)
                && EqualityComparer<T5>.Default.Equals(m_Item5, other.m_Item5)
                && EqualityComparer<T6>.Default.Equals(m_Item6, other.m_Item6)
                && EqualityComparer<T7>.Default.Equals(m_Item7, other.m_Item7)
                && EqualityComparer<T8>.Default.Equals(m_Item8, other.m_Item8)
                && EqualityComparer<T9>.Default.Equals(m_Item9, other.m_Item9)
                && EqualityComparer<T10>.Default.Equals(m_Item10, other.m_Item10)
                && EqualityComparer<T11>.Default.Equals(m_Item11, other.m_Item11)
                && EqualityComparer<T12>.Default.Equals(m_Item12, other.m_Item12)
                && EqualityComparer<T13>.Default.Equals(m_Item13, other.m_Item13)
                && EqualityComparer<T14>.Default.Equals(m_Item14, other.m_Item14)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item3?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item4?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item5?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item6?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item7?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item8?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item9?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item10?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item11?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item12?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item13?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item14?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2}, {Item3}, {Item4}, {Item5}, {Item6}, {Item7}, {Item8}, {Item9}, {Item10}, {Item11}, {Item12}, {Item13}, {Item14})";
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14)
        {
            item1 = m_Item1;
            item2 = m_Item2;
            item3 = m_Item3;
            item4 = m_Item4;
            item5 = m_Item5;
            item6 = m_Item6;
            item7 = m_Item7;
            item8 = m_Item8;
            item9 = m_Item9;
            item10 = m_Item10;
            item11 = m_Item11;
            item12 = m_Item12;
            item13 = m_Item13;
            item14 = m_Item14;
        }

        public static bool operator ==(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;
        private readonly T6 m_Item6;
        private readonly T7 m_Item7;
        private readonly T8 m_Item8;
        private readonly T9 m_Item9;
        private readonly T10 m_Item10;
        private readonly T11 m_Item11;
        private readonly T12 m_Item12;
        private readonly T13 m_Item13;
        private readonly T14 m_Item14;
        private readonly T15 m_Item15;

        public LuminTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15)
        {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
            m_Item6 = item6;
            m_Item7 = item7;
            m_Item8 = item8;
            m_Item9 = item9;
            m_Item10 = item10;
            m_Item11 = item11;
            m_Item12 = item12;
            m_Item13 = item13;
            m_Item14 = item14;
            m_Item15 = item15;
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Create(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public T3 Item3 => m_Item3;

        public T4 Item4 => m_Item4;

        public T5 Item5 => m_Item5;

        public T6 Item6 => m_Item6;

        public T7 Item7 => m_Item7;

        public T8 Item8 => m_Item8;

        public T9 Item9 => m_Item9;

        public T10 Item10 => m_Item10;

        public T11 Item11 => m_Item11;

        public T12 Item12 => m_Item12;

        public T13 Item13 => m_Item13;

        public T14 Item14 => m_Item14;

        public T15 Item15 => m_Item15;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
                && EqualityComparer<T3>.Default.Equals(m_Item3, other.m_Item3)
                && EqualityComparer<T4>.Default.Equals(m_Item4, other.m_Item4)
                && EqualityComparer<T5>.Default.Equals(m_Item5, other.m_Item5)
                && EqualityComparer<T6>.Default.Equals(m_Item6, other.m_Item6)
                && EqualityComparer<T7>.Default.Equals(m_Item7, other.m_Item7)
                && EqualityComparer<T8>.Default.Equals(m_Item8, other.m_Item8)
                && EqualityComparer<T9>.Default.Equals(m_Item9, other.m_Item9)
                && EqualityComparer<T10>.Default.Equals(m_Item10, other.m_Item10)
                && EqualityComparer<T11>.Default.Equals(m_Item11, other.m_Item11)
                && EqualityComparer<T12>.Default.Equals(m_Item12, other.m_Item12)
                && EqualityComparer<T13>.Default.Equals(m_Item13, other.m_Item13)
                && EqualityComparer<T14>.Default.Equals(m_Item14, other.m_Item14)
                && EqualityComparer<T15>.Default.Equals(m_Item15, other.m_Item15)
            ;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item3?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item4?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item5?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item6?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item7?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item8?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item9?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item10?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item11?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item12?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item13?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item14?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item15?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2}, {Item3}, {Item4}, {Item5}, {Item6}, {Item7}, {Item8}, {Item9}, {Item10}, {Item11}, {Item12}, {Item13}, {Item14}, {Item15})";
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15)
        {
            item1 = m_Item1;
            item2 = m_Item2;
            item3 = m_Item3;
            item4 = m_Item4;
            item5 = m_Item5;
            item6 = m_Item6;
            item7 = m_Item7;
            item8 = m_Item8;
            item9 = m_Item9;
            item10 = m_Item10;
            item11 = m_Item11;
            item12 = m_Item12;
            item13 = m_Item13;
            item14 = m_Item14;
            item15 = m_Item15;
        }

        public static bool operator ==(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public struct LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TRest>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;
        private readonly T6 m_Item6;
        private readonly T7 m_Item7;
        private readonly T8 m_Item8;
        private readonly T9 m_Item9;
        private readonly T10 m_Item10;
        private readonly T11 m_Item11;
        private readonly T12 m_Item12;
        private readonly T13 m_Item13;
        private readonly T14 m_Item14;
        private readonly T15 m_Item15;
        private readonly TRest m_Rest;

        public LuminTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15, TRest rest)
        {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
            m_Item6 = item6;
            m_Item7 = item7;
            m_Item8 = item8;
            m_Item9 = item9;
            m_Item10 = item10;
            m_Item11 = item11;
            m_Item12 = item12;
            m_Item13 = item13;
            m_Item14 = item14;
            m_Item15 = item15;
            m_Rest = rest;
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TRest> Create(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15, TRest rest)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TRest>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15, rest);
        }

        public T1 Item1 => m_Item1;

        public T2 Item2 => m_Item2;

        public T3 Item3 => m_Item3;

        public T4 Item4 => m_Item4;

        public T5 Item5 => m_Item5;

        public T6 Item6 => m_Item6;

        public T7 Item7 => m_Item7;

        public T8 Item8 => m_Item8;

        public T9 Item9 => m_Item9;

        public T10 Item10 => m_Item10;

        public T11 Item11 => m_Item11;

        public T12 Item12 => m_Item12;

        public T13 Item13 => m_Item13;

        public T14 Item14 => m_Item14;

        public T15 Item15 => m_Item15;

        public TRest Rest => m_Rest;

        public override bool Equals(object obj)
        {
            return obj is LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TRest> other && Equals(other);
        }

        public bool Equals(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TRest> other)
        {
            return EqualityComparer<T1>.Default.Equals(m_Item1, other.m_Item1)
                && EqualityComparer<T2>.Default.Equals(m_Item2, other.m_Item2)
                && EqualityComparer<T3>.Default.Equals(m_Item3, other.m_Item3)
                && EqualityComparer<T4>.Default.Equals(m_Item4, other.m_Item4)
                && EqualityComparer<T5>.Default.Equals(m_Item5, other.m_Item5)
                && EqualityComparer<T6>.Default.Equals(m_Item6, other.m_Item6)
                && EqualityComparer<T7>.Default.Equals(m_Item7, other.m_Item7)
                && EqualityComparer<T8>.Default.Equals(m_Item8, other.m_Item8)
                && EqualityComparer<T9>.Default.Equals(m_Item9, other.m_Item9)
                && EqualityComparer<T10>.Default.Equals(m_Item10, other.m_Item10)
                && EqualityComparer<T11>.Default.Equals(m_Item11, other.m_Item11)
                && EqualityComparer<T12>.Default.Equals(m_Item12, other.m_Item12)
                && EqualityComparer<T13>.Default.Equals(m_Item13, other.m_Item13)
                && EqualityComparer<T14>.Default.Equals(m_Item14, other.m_Item14)
                && EqualityComparer<T15>.Default.Equals(m_Item15, other.m_Item15)
                && EqualityComparer<TRest>.Default.Equals(m_Rest, other.m_Rest);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (m_Item1?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item2?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item3?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item4?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item5?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item6?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item7?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item8?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item9?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item10?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item11?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item12?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item13?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item14?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Item15?.GetHashCode() ?? 0);
                hash = hash * 23 + (m_Rest?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({Item1}, {Item2}, {Item3}, {Item4}, {Item5}, {Item6}, {Item7}, {Item8}, {Item9}, {Item10}, {Item11}, {Item12}, {Item13}, {Item14}, {Item15}, {Rest})";
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out TRest rest)
        {
            item1 = m_Item1;
            item2 = m_Item2;
            item3 = m_Item3;
            item4 = m_Item4;
            item5 = m_Item5;
            item6 = m_Item6;
            item7 = m_Item7;
            item8 = m_Item8;
            item9 = m_Item9;
            item10 = m_Item10;
            item11 = m_Item11;
            item12 = m_Item12;
            item13 = m_Item13;
            item14 = m_Item14;
            item15 = m_Item15;
            rest = m_Rest;
        }

        public static bool operator ==(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TRest> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TRest> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TRest> left, LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TRest> right)
        {
            return !(left == right);
        }
    }

    public static class LuminTuple
    {

        public static LuminTuple<T1> Create<T1>(T1 item1)
        {
            return new LuminTuple<T1>(item1);
        }

        public static LuminTuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
        {
            return new LuminTuple<T1, T2>(item1, item2);
        }

        public static LuminTuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
        {
            return new LuminTuple<T1, T2, T3>(item1, item2, item3);
        }

        public static LuminTuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            return new LuminTuple<T1, T2, T3, T4>(item1, item2, item3, item4);
        }

        public static LuminTuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            return new LuminTuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8> Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8>(item1, item2, item3, item4, item5, item6, item7, item8);
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>(item1, item2, item3, item4, item5, item6, item7, item8, item9);
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10);
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11);
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12);
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13);
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14);
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15);
        }

        public static LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TRest> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15, TRest rest)
        {
            return new LuminTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TRest>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15, rest);
        }
    }
}
