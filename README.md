* # Maomi.MQ 　　　　　　　　　　　　　　　　　　　　[中文](https://github.com/whuanle/Maomi.MQ/blob/main/README.zh-cn.md)

  **Author:**  whuanle   
  **Documentation URL:** [https://mmq.whuanle.cn](https://mmq.whuanle.cn)    
  **Repository URL:** [https://github.com/whuanle/Maomi.MQ](https://github.com/whuanle/Maomi.MQ)    
  **Author Blogs:**    
  
  * [https://www.whuanle.cn](https://www.whuanle.cn)    
  * [https://www.cnblogs.com/whuanle](https://www.cnblogs.com/whuanle)    
    
  ## Introduction  
  
  Maomi.MQ is a messaging communication model project that currently only supports RabbitMQ. Maomi.MQ.RabbitMQ is a communication model designed specifically for RabbitMQ publishers and consumers, greatly simplifying the code for publishing and messaging. It provides a series of convenient and practical features, allowing developers to achieve high-performance consumption and event orchestration through the framework's consumption model. The framework also supports a range of convenient functionalities, such as publisher confirmation mechanism, custom retry mechanism, compensation mechanism, dead-letter queue, delayed queue, and connection channel reuse. This allows developers to focus more on business logic, simplifying cross-process messaging communication and making cross-process message delivery more straightforward and reliable.   
  
  Additionally, the framework supports distributed observability through built-in runtime APIs, allowing further collection of observability information by using frameworks like OpenTelemetry, which can be pushed to infrastructure platforms.  
  
  ### Table of Contents  
  
  * [Quick Start](https://mmq.whuanle.cn/1.start.html)  
  * [Publishing Events](https://mmq.whuanle.cn/2.publisher.html)  
  * [Consumers](https://mmq.whuanle.cn/2.0.consumer.html)  
    - [Consumer Mode](https://mmq.whuanle.cn/2.1.consumer.html)  
    - [Event Bus Mode](https://mmq.whuanle.cn/2.2.eventbus.html)  
    - [Custom Consumers and Dynamic Subscriptions](https://mmq.whuanle.cn/2.3.dynamic.md)  
  * [Configuration and Debugging](https://mmq.whuanle.cn/3.configuration.html)  
  * [QoS Concurrency and Order](https://mmq.whuanle.cn/4.qos.html)  
  * [Retry](https://mmq.whuanle.cn/5.retry.html)  
  * [Dead Letter Queue](https://mmq.whuanle.cn/6.dead_queue.html)  
  * [Observability](https://mmq.whuanle.cn/7.opentelemetry.html)