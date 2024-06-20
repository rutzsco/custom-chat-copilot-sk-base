# Document Ingestion Monitoring

## Overview

This document provides guidelines and queries for monitoring the RAG ingestion functions on. It is aimed at helping users quickly understand key metrics and set up effective monitoring.

## Dependancy Metrics

## Document Intellegence


- **Errors**: Failed requests
- **Total Calls**: Total number of calls (USAGE)
- **Processed Pages**: Number of pages processed (USAGE)

## Azure Open AI 

- **Azure OpenAI Requsts by Model**: Total number of calls by Model (USAGE)
- **Azure OpenAI Requsts by Status Code**: Total number of calls by StatusCode (FAILURES)

## Workflow Metrics


### Activity Execution Distribution

```kql

let ActivityFunctions = dynamic([
    "check_containers",
    "delete_source_files",
    "generate_extract_embeddings",
    "get_source_files",
    "get_status_record",
    "insert_record",
    "process_pdf_with_document_intelligence",
    "split_pdf_files",
    "update_status_record"
]);

requests
| where name in (ActivityFunctions)
| summarize totalCount=sum(itemCount) by bin(timestamp, 15m), name
| render columnchart 

```

### Trigger Invocation Frequency

```kql

let TriggerFunctions = dynamic([
    "http_start"
]);

requests
| where name in (TriggerFunctions)
| summarize totalCount=sum(itemCount) by bin(timestamp, 15m), name
| render columnchart 

```

### Activity Success Rate

```kql

let ActivityFunctions = dynamic([
    "check_containers",
    "delete_source_files",
    "generate_extract_embeddings",
    "get_source_files",
    "get_status_record",
    "insert_record",
    "process_pdf_with_document_intelligence",
    "split_pdf_files",
    "update_status_record"
]);

requests
| where name in (ActivityFunctions)
| summarize totalCount=sum(itemCount) by  success
| render piechart  

```

### Activity Failures

```kql

let ActivityFunctions = dynamic([
    "check_containers",
    "delete_source_files",
    "generate_extract_embeddings",
    "get_source_files",
    "get_status_record",
    "insert_record",
    "process_pdf_with_document_intelligence",
    "split_pdf_files",
    "update_status_record"
]);

requests
| where name in (ActivityFunctions) and success == false
| summarize totalCount=sum(itemCount) by  name
| render piechart  

```

## Processing Errors

### Errors by operation name

```kql
traces
| where message has "error" or message has "failed" 
| where message !has "BlobNotFound"
| summarize by operation_Name
```

```kql
traces
| where message has "error" or message has "failed" 
| where message !has "BlobNotFound"
| summarize totalCount=sum(itemCount) by bin(timestamp, 15m), operation_Name
| render columnchart
```

### process_pdf_with_document_intelligence

```kql
traces
| where message has "error" or message has "failed" 
| where message !has "BlobNotFound"
| where  operation_Name == "process_pdf_with_document_intelligence"
| summarize by operation_Name
```


### generate_extract_embeddings

```kql
traces
| where message has "error" or message has "failed" 
| where message !has "BlobNotFound"
| where  operation_Name == "generate_extract_embeddings"
| summarize by operation_Name
```