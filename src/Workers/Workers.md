# Work Queues (Task Queues)

A idéia desta abordagem é criar uma fila(Work Queue) que recebe a carga de trabalho (Tasks) e distribui entre trabalhadores (Workers) que irá eventualmente processar estas tasks sem a necessidade de ficar esperando um retorno imediato (síncrono).

Este conceito é especialmente útil em aplicações web onde é impossível lidar com tarefas complexas durante a curta janela de uma requisição HTTP.

Este padrão não precisa de uma exchange específica sendo uma abordagem baseada em filas, podendo usar a Exchange padrão identificada por aspas duplas("") no publish.

```c#
channel.BasicPublish(exchange: "", routingKey: "task_queue", basicProperties: properties, body: body);
```

Uma das maiores vantagens deste padrão é a facilidade em paralelizar carga de trabalho, se apenas um Worker não está suportando o trabalho, basta adicionar uma nova instancia, escalando facilmente. 

## Round-robin dispatching

Por padrão, o Rabbit MQ enviará cada mensagem para o próximo Worker, em sequência. Na média, cada Worker obterá o mesmo número de mensagens. Esta maneira de distribuir mensagens é chamada Round-robin.

## Message Acknowledgment

Uma preocupação importante é garantir que a mensagem foi entregue e processada, imagine o caso em que um Worker fica travado ou o container simplesmente morre, as mensagens em processamento com este worker, morrem juntas, não sendo possível recuperá-las. Se um worker morre, um desejo é que esta mensagem seja enviada à outro worker saudável.

Para garantir que uma mensagem nunca será perdida, o RabbitMQ usa Message Acknowledgment. Um **ack** deve ser enviado pelo worker para o Rabbit comunicando que a mensagem foi recebida, processada com sucesso e que o RabbitMQ está livre para excluir esta mensagem.

### Auto Ack
Existe a possibilidade de realizar autoack, por padrão esta funcionalidade fica desativada, sendo necessário efetuar o Ack manualmente, mas caso queira usar basta adicionar o **autoAck=true** ao consumir a mensagem.

```c#
channel.BasicConsume(queue: "hello", autoAck: true, consumer: consumer);
```

## Message Durability
Para preservar as mensagens na fila mesmo quando o RabbitMQ é reiniciado, derrubado ou apenas parado precisamos marcar as mensagens e a fila como duráveis.

Para fazer isso na fila, basta adicionar o parâmentro durable à declaração da fila e informar true.

```c#
channel.QueueDeclare(queue: "task_queue",durable: true);
```
Agora para fazer isso para as mensagens, criamos um objeto BasicProperties pelo método create, definimos o atributo Persistent como true e adicionamos este objeto na passagem de parametros do método que efetua a publicação da mensagem:

```c#
var properties = channel.CreateBasicProperties();
properties.Persistent = true;//Mark messages as persitent to prevent losses when RabbitMQ restarts.
channel.BasicPublish(exchange: "", routingKey: "task_queue", basicProperties: properties, body: body);
```

## Fair Dispatch
Para um melhor equilíbrio de carga existe uma opção onde é dito para o RabbitMQ não dar mais de uma mensagem para cada Worker por vez. Em outras palavras, não enviar uma nova mensagem para o worker até que o mesmo processe e efetue o ack da mensagem enviada anteriormente, com isso, a mensagem será enviada para o próximo worker disponível.

```c#
channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
```

### Nota sobre Queue Size
Se todos os Workers estão ocupados, sua fila pode ficar cheia. Você precisa fica de olho nisso e talvez, adicionar mais workers.
