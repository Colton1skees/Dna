// Copyright 2014 The Souper Authors. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#include "souper/KVStore/KVStore.h"
//#include "souper/KVStore/KVSocket.h"

#include "llvm/Support/CommandLine.h"
//#include "hiredis.h"

using namespace llvm;
using namespace souper;

static cl::opt<unsigned> RedisPort("souper-redis-port", cl::init(6379),
    cl::desc("Redis server port (default=6379)"));
static cl::opt<bool> UnixSocket("souper-external-cache-unix", cl::init(false),
    cl::desc("Talk to the cache using UNIX domain sockets (default=false)"));

static const int MAX_RETRIES = 5;

namespace souper {

class KVStore::KVImpl {
  //redisContext *Ctx = nullptr;
  int retries = 0;
public:
  KVImpl();
  ~KVImpl();
  void hIncrBy(llvm::StringRef Key, llvm::StringRef Field, int Incr);
  bool hGet(llvm::StringRef Key, llvm::StringRef Field, std::string &Value);
  void hSet(llvm::StringRef Key, llvm::StringRef Field, llvm::StringRef Value);
  void connect();
};

void KVStore::KVImpl::connect() {
  
}

KVStore::KVImpl::KVImpl() {
  connect();
}

KVStore::KVImpl::~KVImpl() {

}

void KVStore::KVImpl::hIncrBy(llvm::StringRef Key, llvm::StringRef Field,
                              int Incr) {

}

bool KVStore::KVImpl::hGet(llvm::StringRef Key, llvm::StringRef Field,
                           std::string &Value) {
  return true;
}

void KVStore::KVImpl::hSet(llvm::StringRef Key, llvm::StringRef Field,
                              llvm::StringRef Value) {
    return;
}

KVStore::KVStore() : Impl (new KVImpl) {}

KVStore::~KVStore() {}

void KVStore::hIncrBy(llvm::StringRef Key, llvm::StringRef Field, int Incr) {
  Impl->hIncrBy(Key, Field, Incr);
}

bool KVStore::hGet(llvm::StringRef Key, llvm::StringRef Field,
                   std::string &Value) {
  return Impl->hGet(Key, Field, Value);
}

void KVStore::hSet(llvm::StringRef Key, llvm::StringRef Field,
                   llvm::StringRef Value) {
  Impl->hSet(Key, Field, Value);
}

}
