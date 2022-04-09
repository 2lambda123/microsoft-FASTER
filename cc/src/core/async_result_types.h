// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#include <atomic>
#include <cstdint>
#include "address.h"
#include "async.h"
#include "native_buffer_pool.h"

#ifdef _WIN32
#include <concurrent_queue.h>

template <typename T>
using concurrent_queue = concurrency::concurrent_queue<T>;
#endif

namespace FASTER {
namespace core {

class AsyncIOContext : public IAsyncContext {
 public:
  AsyncIOContext(void* faster_, Address address_,
                 IAsyncContext* caller_context_,
                 concurrent_queue<AsyncIOContext*>* thread_io_responses_,
                 uint64_t io_id_)
    : faster{ faster_ }
    , address{ address_ }
    , caller_context{ caller_context_ }
    , thread_io_responses{ thread_io_responses_ }
    , io_id{ io_id_ } {
  }
  /// No copy constructor.
  AsyncIOContext(const AsyncIOContext& other) = delete;
  /// The deep-copy constructor.
  AsyncIOContext(AsyncIOContext& other, IAsyncContext* caller_context_)
    : faster{ other.faster }
    , address{ other.address }
    , caller_context{ caller_context_ }
    , thread_io_responses{ other.thread_io_responses }
    , record{ std::move(other.record) }
    , io_id{ other.io_id } {
  }
 protected:
  Status DeepCopy_Internal(IAsyncContext*& context_copy) final {
    return IAsyncContext::DeepCopy_Internal(*this, caller_context, context_copy);
  }
 public:
  void* faster;
  Address address;
  IAsyncContext* caller_context;
  concurrent_queue<AsyncIOContext*>* thread_io_responses;
  uint64_t io_id;

  SectorAlignedMemory record;
};

class AsyncIndexIOContext : public IAsyncContext {
 public:
  AsyncIndexIOContext(void* faster_, KeyHash hash_, IAsyncContext* caller_context_,
                      concurrent_queue<AsyncIndexIOContext*>* thread_io_responses_,
                      uint64_t io_id_)
    : faster{ faster_ }
    , hash{ hash_ }
    , caller_context{ caller_context_ }
    , thread_io_responses{ thread_io_responses_ }
    , io_id{ io_id_ }
    , entry{ HashBucketEntry::kInvalidEntry }
    , atomic_entry{ nullptr } {
  }
  /// No copy constructor.
  AsyncIndexIOContext(const AsyncIOContext& other) = delete;
  /// The deep-copy constructor.
  AsyncIndexIOContext(AsyncIndexIOContext& other, IAsyncContext* caller_context_)
    : faster{ other.faster }
    , caller_context{ caller_context_ }
    , thread_io_responses{ other.thread_io_responses }
    , io_id{ other.io_id }
    , hash{ other.hash }
    , entry{ other.entry }
    , atomic_entry{ other.atomic_entry } {
  }
 protected:
  Status DeepCopy_Internal(IAsyncContext*& context_copy) final {
    return IAsyncContext::DeepCopy_Internal(*this, caller_context, context_copy);
  }

 public:
  void* faster;
  IAsyncContext* caller_context;
  /// Queue where finished pending requests are pushed
  concurrent_queue<AsyncIndexIOContext*>* thread_io_responses;
  /// Unique id for I/O request
  uint64_t io_id;

  KeyHash hash;
  HashBucketEntry entry;
  AtomicHashBucketEntry* atomic_entry;
};


}
} // namespace FASTER::core