namespace Morpeh.Collections {
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Collections.Generic;
    
    public unsafe struct UnmanagedArray<T> :IEnumerable<T> where T : unmanaged {
        private UnsafeStorage<T>* ptr;
        private bool IsUnsafeArrayAllocated => this.ptr != null;
        
        public int Length => this.IsCreated ? ptr->Length : -1;
        public bool IsCreated => IsUnsafeArrayAllocated && ptr->IsCreated;

        public UnmanagedArray(int length) {
            var size = Marshal.SizeOf<UnsafeStorage<T>>();
            this.ptr = (UnsafeStorage<T>*) Marshal.AllocHGlobal(size).ToPointer();
            UnsafeStorageUtils.AllocateUnsafeArray(this.ptr, length);
        }
        
        public T this[int index] {
            get {
                if (!this.IsCreated) {
                    throw new Exception("UnmanagedArray is not created");
                }
                if (index < 0 || index >= this.Length) {
                    throw new IndexOutOfRangeException();
                }
                return ((T*) this.ptr->Ptr.ToPointer())[index];
            }
            
            set {
                if (!this.IsCreated) {
                    throw new Exception("UnmanagedArray is not created");
                }
                if (index < 0 || index >= this.Length) {
                    throw new IndexOutOfRangeException();
                }
                ((T*) this.ptr->Ptr.ToPointer())[index] = value;
            }
        }
        
        public ref T GetElementAsRef(int index) {
            if (!this.IsCreated) {
                throw new Exception("UnmanagedArray is not created");
            }
            if (index < 0 || index >= this.Length) {
                throw new IndexOutOfRangeException();
            }
            return ref ((T*) this.ptr->Ptr.ToPointer())[index];
        }

        public void Resize(int length) {
            if (!this.IsCreated) {
                throw new Exception("UnmanagedArray is not created");
            }
            
            if (length == this.Length) {
                return;
            }
            
            ResizeInternal(this.ptr, length);
        }

        private void ResizeInternal(UnsafeStorage<T>* unsafeArray, int length) {
            var align = Marshal.SizeOf<T>();
            var size = length * align;
            var newPtr = Marshal.AllocHGlobal(size);
            
            if (length > unsafeArray->Length) {
                var oldSize = unsafeArray->Length * align;
                Buffer.MemoryCopy(unsafeArray->Ptr.ToPointer(), newPtr.ToPointer(), size, oldSize);
            }
            else {
                Buffer.MemoryCopy(unsafeArray->Ptr.ToPointer(), newPtr.ToPointer(), size, size);
            }
            
            Marshal.FreeHGlobal(unsafeArray->Ptr);
            unsafeArray->Ptr = newPtr;
            unsafeArray->Capacity = length;
            unsafeArray->Length = length;
        }

        public void Dispose() {
            if (!this.IsUnsafeArrayAllocated) {
                return;
            }
            
            UnsafeStorageUtils.DeallocateUnsafeArray(this.ptr);
            
            Marshal.FreeHGlobal((IntPtr) this.ptr);
            this.ptr = null;
        }

        public IEnumerator<T> GetEnumerator() {
            if (!this.IsCreated) {
                throw new Exception("UnmanagedArray is not created");
            }
            
            for (int i = 0, length = this.Length; i < length; i++) {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}