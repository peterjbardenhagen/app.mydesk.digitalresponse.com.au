<% If isDirector1 Then %>
<script>
// Chart.js initialization for Director Dashboard
const monthlyCtx = document.getElementById('monthlyChart');
if (monthlyCtx) {
    new Chart(monthlyCtx, {
        type: 'line',
        data: {
            labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
            datasets: [
                {
                    label: '<%= currentYear %> Quotes Won',
                    data: [<%= monthlyQuotesThisYear(1) %>, <%= monthlyQuotesThisYear(2) %>, <%= monthlyQuotesThisYear(3) %>, <%= monthlyQuotesThisYear(4) %>, <%= monthlyQuotesThisYear(5) %>, <%= monthlyQuotesThisYear(6) %>, <%= monthlyQuotesThisYear(7) %>, <%= monthlyQuotesThisYear(8) %>, <%= monthlyQuotesThisYear(9) %>, <%= monthlyQuotesThisYear(10) %>, <%= monthlyQuotesThisYear(11) %>, <%= monthlyQuotesThisYear(12) %>],
                    borderColor: '#00a8b5',
                    backgroundColor: 'rgba(0, 168, 181, 0.1)',
                    fill: true,
                    tension: 0.4
                },
                {
                    label: '<%= lastYear %> Quotes Won',
                    data: [<%= monthlyQuotesLastYear(1) %>, <%= monthlyQuotesLastYear(2) %>, <%= monthlyQuotesLastYear(3) %>, <%= monthlyQuotesLastYear(4) %>, <%= monthlyQuotesLastYear(5) %>, <%= monthlyQuotesLastYear(6) %>, <%= monthlyQuotesLastYear(7) %>, <%= monthlyQuotesLastYear(8) %>, <%= monthlyQuotesLastYear(9) %>, <%= monthlyQuotesLastYear(10) %>, <%= monthlyQuotesLastYear(11) %>, <%= monthlyQuotesLastYear(12) %>],
                    borderColor: '#d4a574',
                    backgroundColor: 'rgba(212, 165, 116, 0.1)',
                    fill: true,
                    tension: 0.4
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        usePointStyle: true,
                        padding: 20
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function(value) {
                            return '$' + value.toLocaleString();
                        }
                    }
                }
            }
        }
    });
}

const revenueCtx = document.getElementById('revenueChart');
if (revenueCtx) {
    new Chart(revenueCtx, {
        type: 'doughnut',
        data: {
            labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
            datasets: [{
                data: [<%= monthlyInvoicesThisYear(1) %>, <%= monthlyInvoicesThisYear(2) %>, <%= monthlyInvoicesThisYear(3) %>, <%= monthlyInvoicesThisYear(4) %>, <%= monthlyInvoicesThisYear(5) %>, <%= monthlyInvoicesThisYear(6) %>, <%= monthlyInvoicesThisYear(7) %>, <%= monthlyInvoicesThisYear(8) %>, <%= monthlyInvoicesThisYear(9) %>, <%= monthlyInvoicesThisYear(10) %>, <%= monthlyInvoicesThisYear(11) %>, <%= monthlyInvoicesThisYear(12) %>],
                backgroundColor: [
                    '#00a8b5', '#d4a574', '#667eea', '#11998e', '#38ef7d', 
                    '#00c4d3', '#e8c088', '#764ba2', '#f093fb', '#f5576c', 
                    '#4facfe', '#00f2fe'
                ],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        usePointStyle: true,
                        padding: 15
                    }
                }
            },
            cutout: '60%'
        }
    });
}
</script>
<% End If %>
