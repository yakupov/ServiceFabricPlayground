# Intro
This is a PoC sharded KV store that runs on SF

# Services
ApiService - stateless. Provides REST APIs for get and put, calls StorageService

StorageService - stateful, partitioned and replicated (by SF), stores data in C:\temp in a very inefficient way :)

# APIs
GET localhost:port/store/{key}

PUT localhost:port/store/{key} with content-type=text/plain

Port is assigned dynamically during startup (see logs)

