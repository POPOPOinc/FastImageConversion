use crate::ByteBuffer;

impl Copy for ByteBuffer {}

impl Clone for ByteBuffer {
    fn clone(&self) -> ByteBuffer {
        *self
    }
}

impl ByteBuffer {
    pub fn from_vec(bytes: Vec<u8>) -> Self {
        let length = i32::try_from(bytes.len()).expect("buffer length cannot fit into a i32.");
        let capacity = i32::try_from(bytes.capacity()).expect("buffer capacity cannot fit into a i32.");

        // keep memory until call delete
        let mut v = std::mem::ManuallyDrop::new(bytes);

        Self { ptr: v.as_mut_ptr(), length, capacity }
    }

    pub fn destroy_into_vec(self) -> Vec<u8> {
        if self.ptr.is_null() {
            vec![]
        } else {
            let capacity: usize = self
                .capacity
                .try_into()
                .expect("buffer capacity negative or overflowed");
            let length: usize = self
                .length
                .try_into()
                .expect("buffer length negative or overflowed");

            unsafe { Vec::from_raw_parts(self.ptr, length, capacity) }
        }
    }

    pub fn destroy(self) {
        drop(self.destroy_into_vec());
    }
}
